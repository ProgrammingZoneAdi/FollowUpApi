using FollowUpApi.Common;
using FollowUpApi.DataContext;
using FollowUpApi.DataContext.Entities;
using FollowUpApi.Domain.Interfaces;
using FollowUpApi.Features.Courses;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;

namespace FollowUpApi.Domain.Managers;

public class CourseManager  : ICourseManager
{
    private const string ActiveStatus = "Active";
    private const string InactiveStatus = "Inactive";
    private static readonly string[] CourseManagementRoles = ["Owner", "Admin"];

    private readonly AppDbContext _dbContext;
    private readonly ICompanyAccessService _companyAccessService;
    private readonly ILogger<CourseManager> _logger;

    public CourseManager(AppDbContext dbContext, ICompanyAccessService companyAccessService, ILogger<CourseManager> logger)
    {
        _dbContext = dbContext;
        _companyAccessService = companyAccessService;
        _logger = logger;
    }

    public async Task<ServiceResult<CourseResponse>> CreateCourseAsync(Guid companyId, Guid requestedByUserId, CreateCourseRequest request , CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, CourseManagementRoles, cancellationToken);

        if(access.Status == CompanyAccessStatus.CompanyNotFound)
        {
            return ServiceResult<CourseResponse>.NotFound("Company was not found");
        }

        if(access.Status == CompanyAccessStatus.Forbidden)
        {
            return ServiceResult<CourseResponse>.Forbidden("Only a company owner or admin can create courses");
        }

        var courseName = request.CourseName?.Trim() ?? string.Empty;
        var duration = request.Duration?.Trim() ?? string.Empty;
        var fees = request.Fees;

        var validationError = ValidateCourse(courseName, duration, fees);

        if(validationError is not null)
        {
            return ServiceResult<CourseResponse>.ValidationError(validationError);
        }

        try
        {
            var courseAlreadyExists = await _dbContext.Courses.AsNoTracking().AnyAsync(course => !course.IsDeleted && course.CompanyId == companyId && EF.Functions.ILike(course.CourseName, courseName), cancellationToken);

            if (courseAlreadyExists)
            {
                return ServiceResult<CourseResponse>.Conflict("A course with the same name already exists in this company");
            }

            var course = new Course {
                CompanyId = companyId,
                CourseName = courseName,
                Fees = fees,
                Duration = duration,
                Status = ActiveStatus,
                CreatedBy = requestedByUserId
            };

            await _dbContext.Courses.AddAsync(course, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Course {CourseId} created in company {CompanyId} by {RequestedByUserId}", course.Id, companyId, requestedByUserId);

            return ServiceResult<CourseResponse>.Ok(MapCourse(course), "Course created successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch(DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Database conflict while creating course {CourseName} in company {CompanyId}", courseName, companyId);

            return ServiceResult<CourseResponse>.Conflict("Course could not be created because the data conflicts with an existing record");
        }
        catch(Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while creating course {CourseName} in company {CompanyId}", courseName, companyId);

            return ServiceResult<CourseResponse>.Error("An unexpected error occurred while creating the course");
        }
    }

    public async Task<ServiceResult<PaginationResponse<CourseResponse>>> GetCoursesAsync(Guid companyId, Guid requestedByUserId, GetCoursesRequest request, CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, Array.Empty<string>(), cancellationToken);

        if (access.Status == CompanyAccessStatus.CompanyNotFound)
        {
            return ServiceResult<PaginationResponse<CourseResponse>>.NotFound("Company was not found");
        }

        if (access.Status == CompanyAccessStatus.Forbidden)
        {
            return ServiceResult<PaginationResponse<CourseResponse>>.Forbidden("You are not an active member of this company");
        }

        var pageNumber = request.PageNumber;
        var pageSize = request.PageSize;
        var search = request.Search?.Trim();
        var requestedStatus = request.Status?.Trim();

        if (pageNumber < 1)
        {
            return ServiceResult<PaginationResponse<CourseResponse>>.ValidationError("Page number must be greater than zero");
        }

        if (pageSize is < 1 or > 100)
        {
            return ServiceResult<PaginationResponse<CourseResponse>>.ValidationError("Page size must be between 1 and 100");
        }

        if (!string.IsNullOrWhiteSpace(search) && search.Length > 100)
        {
            return ServiceResult<PaginationResponse<CourseResponse>>.ValidationError("Search text cannot exceed 100 characters");
        }

        if ((long)(pageNumber - 1) * pageSize > int.MaxValue)
        {
            return ServiceResult<PaginationResponse<CourseResponse>>.ValidationError("Requested page is too large");
        }

        string? statusFilter;

        if (string.IsNullOrWhiteSpace(requestedStatus) || string.Equals(requestedStatus, ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            statusFilter = ActiveStatus;
        }
        else if(string.Equals(requestedStatus, "Inactive", StringComparison.OrdinalIgnoreCase))
        {
            statusFilter = "Inactive";
        }
        else if(string.Equals(requestedStatus, "All", StringComparison.OrdinalIgnoreCase))
        {
            statusFilter = null;
        }
        else
        {
            return ServiceResult<PaginationResponse<CourseResponse>>.ValidationError("Status must be Active, Inactive, or All");
        }

        try
        {
            var query = _dbContext.Courses.AsNoTracking().Where(course => !course.IsDeleted && course.CompanyId == companyId);

            if(statusFilter is not null)
            {
                query = query.Where(course => course.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchPattern = $"%{search}%";

                query = query.Where(course => EF.Functions.ILike(course.CourseName, searchPattern) || EF.Functions.ILike(course.Duration, searchPattern));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var rows = await query.OrderBy(course => course.CourseName).ThenBy(course => course.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize)
                .Select(course => new CourseResponse
                {
                    CourseId = course.Id,
                    CompanyId = course.CompanyId,
                    CourseName = course.CourseName,
                    Fees = course.Fees,
                    Duration = course.Duration,
                    Status = course.Status,
                    CreatedOn = course.CreatedOn,
                    UpdatedOn = course.UpdatedOn
                }).ToListAsync(cancellationToken);

            var response = new PaginationResponse<CourseResponse>
            {
                Rows = rows,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ServiceResult<PaginationResponse<CourseResponse>>.Ok(response, "Courses retrieved successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch(Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while retrieving courses for company {CompanyId}", companyId);

            return ServiceResult<PaginationResponse<CourseResponse>>.Error("An unexpected error occured while retrieving courses");
        }
    }

    public async Task<ServiceResult<CourseResponse>> UpdateCourseAsync(Guid companyId, Guid courseId, Guid requestedByUserId , UpdateCourseRequest request, CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, CourseManagementRoles, cancellationToken);

        if(access.Status == CompanyAccessStatus.CompanyNotFound)
        {
            return ServiceResult<CourseResponse>.NotFound("Company was not found");
        }

        if(access.Status == CompanyAccessStatus.Forbidden)
        {
            return ServiceResult<CourseResponse>.Forbidden("Only a company owner or admin can update courses");
        }

        var courseName = request.CourseName?.Trim() ?? string.Empty;
        var duration = request.Duration?.Trim() ?? string.Empty;
        var fees = request.Fees;

        var validationError = ValidateCourse(courseName, duration, fees);

        if(validationError is not null)
        {
            return ServiceResult<CourseResponse>.ValidationError(validationError);
        }

        try
        {
            var course = await _dbContext.Courses.FirstOrDefaultAsync(item => !item.IsDeleted && item.CompanyId == companyId && item.Id == courseId, cancellationToken);

            if(course is null)
            {
                return ServiceResult<CourseResponse>.NotFound("Course was not found");
            }

            var duplicateNameExists = await _dbContext.Courses.AsNoTracking().AnyAsync(item => !item.IsDeleted && item.CompanyId == companyId && item.Id != courseId && EF.Functions.ILike(item.CourseName, courseName), cancellationToken);

            if (duplicateNameExists)
            {
                return ServiceResult<CourseResponse>.Conflict("A course with the same name already exists in this company");
            }

            course.CourseName = courseName;
            course.Fees = fees;
            course.Duration = duration;
            course.UpdatedOn = DateTime.UtcNow;
            course.UpdatedBy = requestedByUserId;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Course {CourseId} updated in company {CompanyId} by {RequestedByUserId}", courseId, companyId, requestedByUserId);

            return ServiceResult<CourseResponse>.Ok(MapCourse(course), "Course updated successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(
                exception,
                "Database conflict while updating course {CourseId} in company {CompanyId}",
                courseId,
                companyId);

            return ServiceResult<CourseResponse>.Conflict(
                "Course could not be updated because the data conflicts with an existing record");
        }
        catch(Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while updating course {CourseId} in company {CompanyId}", courseId, companyId);

            return ServiceResult<CourseResponse>.Error("An unexpected error occurred while updating the course");
        }
    }

    public async Task<ServiceResult<CourseResponse>> DeactivateCourseAsync(Guid companyId, Guid courseId, Guid requestedByUserId, CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, CourseManagementRoles, cancellationToken);

        if(access.Status == CompanyAccessStatus.CompanyNotFound)
        {
            return ServiceResult<CourseResponse>.NotFound("Company was not found");
        }

        if(access.Status == CompanyAccessStatus.Forbidden)
        {
            return ServiceResult<CourseResponse>.Forbidden("Only a company owner or admin can deactivate courses");
        }

        try
        {
            var course = await _dbContext.Courses.FirstOrDefaultAsync(item => !item.IsDeleted && item.CompanyId == companyId && item.Id == courseId, cancellationToken);

            if(course is null)
            {
                return ServiceResult<CourseResponse>.NotFound("Course was not found");
            }

            if(string.Equals(course.Status, InactiveStatus, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<CourseResponse>.Ok(MapCourse(course), "Course is already inactive");
            }

            course.Status = InactiveStatus;
            course.UpdatedOn = DateTime.UtcNow;
            course.UpdatedBy = requestedByUserId;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Course {CourseId} deactivated in company {CompanyId} by {RequestedByUserId}", courseId, companyId, requestedByUserId);

            return ServiceResult<CourseResponse>.Ok(MapCourse(course), "Course deactivated successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch(DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Database conflict while deactivating course {CourseId} in company {CompanyId}", courseId, companyId);

            return ServiceResult<CourseResponse>.Conflict("Course could not be deactivated because the data conflicts with an existing record");
        }
        catch(Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while deactivating course {CourseId} in company {CompanyId}", courseId, companyId);

            return ServiceResult<CourseResponse>.Error("An unexpected error occurred while deactivating the course");
        }

    }
    private static string? ValidateCourse(string courseName, string duration, decimal? fees)
    {
        if (string.IsNullOrWhiteSpace(courseName))
            return "Course name is required";

        if (courseName.Length > 200)
            return "Course name cannot exceed 200 characters";

        if (string.IsNullOrWhiteSpace(duration))
            return "Course duration is required";

        if (duration.Length > 100)
            return "Course duration cannot exceed 100 characters";

        if (fees is < 0)
            return "Course fees cannot be negative";

        if (fees is > 100_000_000m)
            return "Course fees cannot exceed 100000000";

        return null;
    }

    private static CourseResponse MapCourse(Course course)
    {
        return new CourseResponse
        {
            CourseId = course.Id,
            CompanyId = course.CompanyId,
            CourseName = course.CourseName,
            Fees = course.Fees,
            Duration = course.Duration,
            Status = course.Status,
            CreatedOn = course.CreatedOn,
            UpdatedOn = course.UpdatedOn
        };
    }
}
