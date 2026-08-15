using FollowUpApi.Common;
using FollowUpApi.DataContext;
using FollowUpApi.DataContext.Entities;
using FollowUpApi.Domain.Interfaces;
using FollowUpApi.Features.LeadSources;
using Microsoft.EntityFrameworkCore;

namespace FollowUpApi.Domain.Managers;

public class LeadSourceManager : ILeadSourceManager
{
    private const string ActiveStatus = "Active";
    private static readonly string[] LeadSourceManagementRoles = ["Owner", "Admin"];

    private readonly AppDbContext _dbContext;
    private readonly ICompanyAccessService _companyAccessService;
    private readonly ILogger<LeadSourceManager> _logger;

    public LeadSourceManager(AppDbContext dbContext, ICompanyAccessService companyAccessService, ILogger<LeadSourceManager> logger)
    {
        _dbContext = dbContext;
        _companyAccessService = companyAccessService;
        _logger = logger;
        
    }

    public async Task<ServiceResult<LeadSourceResponse>> CreateLeadSourceAsync(Guid companyId, Guid requestedByUserId, CreateLeadSourceRequest request, CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, LeadSourceManagementRoles, cancellationToken);

        if(access.Status == CompanyAccessStatus.CompanyNotFound)
        {
            return ServiceResult<LeadSourceResponse>.NotFound("Company was not found");
        }

        if(access.Status == CompanyAccessStatus.Forbidden)
        {
            return ServiceResult<LeadSourceResponse>.Forbidden("Only a company owner or admin can create lead sources");
        }

        var sourceName = request.SourceName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return ServiceResult<LeadSourceResponse>.ValidationError("Source name is required");
        }

        if(sourceName.Length > 150)
        {
            return ServiceResult<LeadSourceResponse>.ValidationError("Source name cannot exceed 150 characters");
        }

        try
        {
            var sourceAlreadyExists = await _dbContext.LeadSources.AsNoTracking().AnyAsync(source => !source.IsDeleted && source.CompanyId == companyId && EF.Functions.ILike(source.SourceName, sourceName), cancellationToken);

            if (sourceAlreadyExists)
            {
                return ServiceResult<LeadSourceResponse>.Conflict("A lead source with the same name already exists in this company");
            }

            var leadSource = new LeadSource
            {
                CompanyId = companyId,
                SourceName = sourceName,
                Status = ActiveStatus,
                CreatedBy = requestedByUserId
            };

            await _dbContext.LeadSources.AddAsync(leadSource, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Lead source {SourceId} created in company {CompanyId} by {RequestedByUserId}", leadSource.Id, companyId, requestedByUserId);

            return ServiceResult<LeadSourceResponse>.Ok(MapLeadSource(leadSource), "Lead source created successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch(DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Database conflict while creating lead source {SourceName} in company {CompanyId}", sourceName, companyId);

            return ServiceResult<LeadSourceResponse>.Conflict("Lead source could not be created because the data conflicts with an existing record");
        }
        catch(Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while creating lead source {SourceName} in company {CompanyId}", sourceName, companyId);

            return ServiceResult<LeadSourceResponse>.Error("An unexpected error occured while creating the lead source");
        }

    }

    public async Task<ServiceResult<PaginationResponse<LeadSourceResponse>>> GetLeadSourcesAsync(Guid companyId, Guid requestedByUserId, GetLeadSourcesRequest request, CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, Array.Empty<string>(), cancellationToken);

        if(access.Status == CompanyAccessStatus.CompanyNotFound)
        {
            return ServiceResult<PaginationResponse<LeadSourceResponse>>.NotFound("Company was not found");
        }

        if(access.Status == CompanyAccessStatus.Forbidden)
        {
            return ServiceResult<PaginationResponse<LeadSourceResponse>>.Forbidden("You are not an active member of this company");
        }

        var pageNumber = request.PageNumber;
        var pageSize = request.PageSize;
        var search = request.Search?.Trim();
        var requestedStatus = request.Status?.Trim();

        if(pageNumber < 1)
        {
            return ServiceResult<PaginationResponse<LeadSourceResponse>>.ValidationError("Page number must be greater than zero");
        }

        if(pageSize is < 1 or > 100)
        {
            return ServiceResult<PaginationResponse<LeadSourceResponse>>.ValidationError("Page size must be between 1 and 100");
        }

        if(!string.IsNullOrWhiteSpace(search) && search.Length > 100)
        {
            return ServiceResult<PaginationResponse<LeadSourceResponse>>.ValidationError("Search text cannot exceed 100 characters");
        }

        if((long)(pageNumber - 1) * pageSize > int.MaxValue)
        {
            return ServiceResult<PaginationResponse<LeadSourceResponse>>.ValidationError("Requested page is too large");
        }

        string? statusFilter;

        if(string.IsNullOrWhiteSpace(requestedStatus) || string.Equals(requestedStatus, ActiveStatus, StringComparison.OrdinalIgnoreCase))
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
            return ServiceResult<PaginationResponse<LeadSourceResponse>>.ValidationError("Status must be Active, Inactive or All");
        }

        try
        {
            var query = _dbContext.LeadSources.AsNoTracking().Where(source => !source.IsDeleted && source.CompanyId == companyId);

            if(statusFilter is not null)
            {
                query = query.Where(source => source.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchPattern = $"%{search}%";

                query = query.Where(source => EF.Functions.ILike(source.SourceName, searchPattern));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var rows = await query.OrderBy(source => source.SourceName).ThenBy(source => source.Id)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                .Select(source => new LeadSourceResponse
                {
                    SourceId = source.Id,
                    CompanyId = source.CompanyId,
                    SourceName = source.SourceName,
                    Status = source.Status,
                    CreatedOn = source.CreatedOn,
                    UpdatedOn = source.UpdatedOn
                }).ToListAsync(cancellationToken);

            var response = new PaginationResponse<LeadSourceResponse>
            {
                Rows = rows,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ServiceResult<PaginationResponse<LeadSourceResponse>>.Ok(response, "Lead sources retrieved successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch(Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while retrieving lead sources for company {CompanyId}", companyId);

            return ServiceResult<PaginationResponse<LeadSourceResponse>>.Error("An unexpected error occured while retrieving lead sources");
        }

    }

    public async Task<ServiceResult<LeadSourceResponse>> UpdateLeadSourceAsync(Guid companyId, Guid sourceId, Guid requestedByUserId, UpdateLeadSourceRequest request, CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, LeadSourceManagementRoles, cancellationToken);
        
        if(access.Status == CompanyAccessStatus.CompanyNotFound)
        {
            return ServiceResult<LeadSourceResponse>.NotFound("Company was not found");
        }

        if(access.Status == CompanyAccessStatus.Forbidden)
        {
            return ServiceResult<LeadSourceResponse>.Forbidden("Only a company owner or admin can update lead sources");
        }

        var sourceName = request.SourceName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return ServiceResult<LeadSourceResponse>.ValidationError("Source name is required");
        }

        if(sourceName.Length > 150)
        {
            return ServiceResult<LeadSourceResponse>.ValidationError("Source name cannot exceed 150 characters");
        }

        try
        {
            var leadSource = await _dbContext.LeadSources.FirstOrDefaultAsync(source => !source.IsDeleted && source.CompanyId == companyId && source.Id == sourceId, cancellationToken);

            if(leadSource is null)
            {
                return ServiceResult<LeadSourceResponse>.NotFound("Lead source was not found");
            }

            var duplicateNameExists = await _dbContext.LeadSources.AsNoTracking().AnyAsync(source => !source.IsDeleted && source.CompanyId == companyId && source.Id != sourceId && EF.Functions.ILike(source.SourceName, sourceName), cancellationToken);

            if (duplicateNameExists)
            {
                return ServiceResult<LeadSourceResponse>.Conflict("A lead source with the same name already exists in this company");
            }

            leadSource.SourceName = sourceName;
            leadSource.UpdatedOn = DateTime.UtcNow;
            leadSource.UpdatedBy = requestedByUserId;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Lead source {SourceId} updated in company {CompanyId} by {RequestedByUserId}", sourceId, companyId, requestedByUserId);

            return ServiceResult<LeadSourceResponse>.Ok(MapLeadSource(leadSource), "Lead source updated successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch(DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Database conflict while updating lead source {SourceId} in company {CompanyId}", sourceId, companyId);

            return ServiceResult<LeadSourceResponse>.Conflict("Lead source could not be updated because the data conflicts with an existing record");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unexpected error while updating lead source {SourceId} in company {CompanyId}", sourceId, companyId);

            return ServiceResult<LeadSourceResponse>.Error("An unexpected error occured while updating the lead source");
        }
    }

    private static LeadSourceResponse MapLeadSource(LeadSource leadSource)
    {
        return new LeadSourceResponse
        {
            SourceId = leadSource.Id,
            CompanyId = leadSource.CompanyId,
            SourceName = leadSource.SourceName,
            Status = leadSource.Status,
            CreatedOn = leadSource.CreatedOn,
            UpdatedOn = leadSource.UpdatedOn
        };
    }
}