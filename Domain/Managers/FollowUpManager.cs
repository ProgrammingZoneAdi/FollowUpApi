using FollowUpApi.DataContext;
using FollowUpApi.Domain.Interfaces;
using FollowUpApi.Common;
using FollowUpApi.DataContext.Entities;
using FollowUpApi.Features.FollowUps;
using Microsoft.EntityFrameworkCore;
using FollowUpApi.Features.Leads;

namespace FollowUpApi.Domain.Managers;

public class FollowUpManager : IFollowUpManager
{
    private readonly AppDbContext _dbContext;
    private readonly ICompanyAccessService _companyAccessService;
    private readonly ILogger<FollowUpManager> _logger;
    private static readonly string[] AllowedTypes = ["Call", "WhatsApp", "Email", "Meeting", "SMS", "Other"];
    private static readonly string[] AllowedStatuses = ["New", "Contacted", "Follow-up", "Qualified", "Won", "Lost"];

    public FollowUpManager(AppDbContext dbContext, ICompanyAccessService companyAccessService, ILogger<FollowUpManager> logger)
    {
        _dbContext = dbContext;
        _companyAccessService = companyAccessService;
        _logger = logger;
    }

    public async Task<ServiceResult<PaginationResponse<FollowUpResponse>>> GetFollowUpsAsync(Guid companyId, Guid leadId, Guid requestedByUserId, GetFollowUpsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, ["Owner", "Admin", "Counsellor", "Staff"], cancellationToken);

            if (access.Status == CompanyAccessStatus.CompanyNotFound)
            {
                return ServiceResult<PaginationResponse<FollowUpResponse>>.NotFound("Company was not found");
            }

            if (access.Status != CompanyAccessStatus.Granted)
            {
                return ServiceResult<PaginationResponse<FollowUpResponse>>.Forbidden("You cannot view follow-ups in this company");
            }

            var page = request.PageNumber;
            var pageSize = request.PageSize;

            if (page < 1)
            {
                return ServiceResult<PaginationResponse<FollowUpResponse>>.ValidationError("Page number must be greater than zero");
            }

            if (pageSize is < 1 or > 100)
            {
                return ServiceResult<PaginationResponse<FollowUpResponse>>.ValidationError("Page size must be between 1 and 100");
            }

            var skip = (long)(page - 1) * pageSize;

            if (skip > int.MaxValue)
            {
                return ServiceResult<PaginationResponse<FollowUpResponse>>.ValidationError("Requested page is too large");
            }

            var canViewAll = string.Equals(access.Role, "Owner", StringComparison.OrdinalIgnoreCase) || string.Equals(access.Role, "Admin", StringComparison.OrdinalIgnoreCase);

            var accessibleLead = _dbContext.Leads.AsNoTracking().Where(lead => !lead.IsDeleted && lead.CompanyId == companyId && lead.Id == leadId
                && (canViewAll || lead.AssignedToUserId == requestedByUserId));

            if (!await accessibleLead.AnyAsync(cancellationToken))
            {
                return ServiceResult<PaginationResponse<FollowUpResponse>>.NotFound("Lead was not found");
            }

            var query = _dbContext.LeadFollowUps.AsNoTracking().Where(item => !item.IsDeleted && item.CompanyId == companyId && item.LeadId == leadId
                && accessibleLead.Any(lead => lead.Id == item.LeadId));

            var totalCount = await query.CountAsync(cancellationToken);

            var rows = await query
                .OrderByDescending(item => item.FollowUpDate)
                .ThenByDescending(item => item.CreatedOn)
                .ThenByDescending(item => item.Id)
                .Skip((int)skip)
                .Take(pageSize)
                .Select(item => new FollowUpResponse
                {
                    FollowUpId = item.Id,
                    CompanyId = item.CompanyId,
                    LeadId = item.LeadId,
                    FollowUpType = item.FollowUpType,
                    Remark = item.Remark,
                    StatusAfterFollowUp = item.StatusAfterFollowUp,
                    FollowUpDate = item.FollowUpDate,
                    NextFollowUpDate = item.NextFollowUpDate,
                    CreatedBy = item.CreatedBy,
                    CreatedOn = item.CreatedOn
                })
                .ToListAsync(cancellationToken);

            var response = new PaginationResponse<FollowUpResponse>
            {
                Rows = rows,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return ServiceResult<PaginationResponse<FollowUpResponse>>.Ok(response, "Follow-up history retrieved successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error retrieving follow-ups for lead {LeadId} in company {CompanyId}", leadId, companyId);

            return ServiceResult<PaginationResponse<FollowUpResponse>>.Error("An unexpected error occurred while retrieving follow-up history");
        }
    }

    public async Task<ServiceResult<FollowUpResponse>> AddFollowUpAsync(Guid companyId, Guid leadId, Guid requestedByUserId, AddFollowUpRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, ["Owner", "Admin", "Counsellor", "Staff"], cancellationToken);
            if (access.Status == CompanyAccessStatus.CompanyNotFound)
            {
                return ServiceResult<FollowUpResponse>.NotFound("Company was not found");
            }

            if (access.Status != CompanyAccessStatus.Granted)
            {
                return ServiceResult<FollowUpResponse>.Forbidden("You cannot add follow-ups in this company");
            }

            var type = AllowedTypes.FirstOrDefault(value => string.Equals(value,
                request.FollowUpType?.Trim(), StringComparison.OrdinalIgnoreCase));
            var status = AllowedStatuses.FirstOrDefault(value => string.Equals(value,
                request.StatusAfterFollowUp?.Trim(), StringComparison.OrdinalIgnoreCase));
            var remark = request.Remark?.Trim() ?? string.Empty;
            var lostReason = request.LostReason?.Trim() ?? string.Empty;
            var nextDate = request.NextFollowUpDate?.UtcDateTime;

            if (type is null)
            {
                return ServiceResult<FollowUpResponse>.ValidationError("Follow-up type must be Call, WhatsApp, Email, Meeting, SMS, or Other");
            }

            if (status is null)
            {
                return ServiceResult<FollowUpResponse>.ValidationError("Status must be New, Contacted, Follow-up, Qualified, Won, or Lost");
            }

            if (string.IsNullOrWhiteSpace(remark) || remark.Length > 2000)
            {
                return ServiceResult<FollowUpResponse>.ValidationError("Remark is required and cannot exceed 2000 characters");
            }

            if (lostReason.Length > 1000)
            {
                return ServiceResult<FollowUpResponse>.ValidationError("Lost reason cannot exceed 1000 characters");
            }

            if (status == "Lost" && string.IsNullOrWhiteSpace(lostReason))
            {
                return ServiceResult<FollowUpResponse>.ValidationError("Lost reason is required when status is Lost");
            }

            if (status != "Lost" && lostReason.Length > 0)
            {
                return ServiceResult<FollowUpResponse>.ValidationError("Lost reason can only be supplied for Lost status");
            }

            if ((status is "Won" or "Lost") && nextDate.HasValue)
            {
                return ServiceResult<FollowUpResponse>.ValidationError("Next follow-up date must be null for Won or Lost status");
            }

            if (status == "Follow-up" && !nextDate.HasValue)
            {
                return ServiceResult<FollowUpResponse>.ValidationError("Next follow-up date is required for Follow-up status");
            }

            // Keep the interaction, lead summary and history consistent under concurrent requests.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);
            var canManageAll = string.Equals(access.Role, "Owner", StringComparison.OrdinalIgnoreCase)
                || string.Equals(access.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            var lead = await _dbContext.Leads.FirstOrDefaultAsync(item =>
                !item.IsDeleted && item.CompanyId == companyId && item.Id == leadId
                && (canManageAll || item.AssignedToUserId == requestedByUserId), cancellationToken);
            if (lead is null)
            {
                return ServiceResult<FollowUpResponse>.NotFound("Lead was not found");
            }

            if (lead.Status is "Won" or "Lost")
            {
                return ServiceResult<FollowUpResponse>.Conflict("Reopen the lead using the status API before adding a follow-up");
            }

            var now = DateTime.UtcNow;
            if (nextDate.HasValue && nextDate.Value <= now)
            {
                return ServiceResult<FollowUpResponse>.ValidationError("Next follow-up date must be in the future");
            }

            var followUp = new LeadFollowUp
            {
                CompanyId = companyId,
                LeadId = leadId,
                FollowUpType = type,
                Remark = remark,
                StatusAfterFollowUp = status,
                FollowUpDate = now,
                NextFollowUpDate = nextDate,
                CreatedOn = now,
                CreatedBy = requestedByUserId
            };
            if (lead.Status != status || lead.LostReason != lostReason)
            {
                _dbContext.LeadStatusHistories.Add(new LeadStatusHistory
                {
                    CompanyId = companyId,
                    LeadId = leadId,
                    OldStatus = lead.Status,
                    NewStatus = status,
                    Remarks = status == "Lost" ? $"Lost reason: {lostReason}\n{remark}" : remark,
                    CreatedOn = now,
                    CreatedBy = requestedByUserId
                });
            }

            lead.Status = status;
            lead.LostReason = lostReason;
            lead.LastFollowUpDate = now;
            lead.NextFollowUpDate = nextDate;
            lead.UpdatedOn = now;
            lead.UpdatedBy = requestedByUserId;
            _dbContext.LeadFollowUps.Add(followUp);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Follow-up {FollowUpId} added to lead {LeadId} in company {CompanyId} by {UserId}",
                followUp.Id, leadId, companyId, requestedByUserId);
            return ServiceResult<FollowUpResponse>.Ok(new FollowUpResponse
            {
                FollowUpId = followUp.Id,
                CompanyId = companyId,
                LeadId = leadId,
                FollowUpType = type,
                Remark = remark,
                StatusAfterFollowUp = status,
                FollowUpDate = now,
                NextFollowUpDate = nextDate,
                CreatedBy = requestedByUserId,
                CreatedOn = now
            }, "Follow-up added successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Npgsql.PostgresException exception) when (exception.SqlState is "40001" or "40P01")
        {
            _logger.LogWarning(exception, "Concurrent update while adding follow-up to lead {LeadId}", leadId);
            return ServiceResult<FollowUpResponse>.Conflict("Lead changed while saving. Reload its details and retry");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Database conflict adding follow-up to lead {LeadId}", leadId);
            return ServiceResult<FollowUpResponse>.Conflict("Follow-up could not be saved. Reload lead details and retry");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error adding follow-up to lead {LeadId} in company {CompanyId}", leadId, companyId);
            return ServiceResult<FollowUpResponse>.Error("An unexpected error occurred while adding the follow-up");
        }
    }
    public async Task<ServiceResult<PaginationResponse<LeadResponse>>> GetOverdueFollowUpsAsync(Guid companyId, Guid requestedByUserId, GetOverdueFollowUpsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, ["Owner", "Admin", "Counsellor", "Staff"], cancellationToken);

            if (access.Status == CompanyAccessStatus.CompanyNotFound)
            {
                return ServiceResult<PaginationResponse<LeadResponse>>.NotFound("Company was not found");
            }

            if (access.Status != CompanyAccessStatus.Granted)
            {
                return ServiceResult<PaginationResponse<LeadResponse>>.Forbidden("You cannot view follow-ups in this company");
            }

            var page = request.PageNumber;
            var pageSize = request.PageSize;

            if (page < 1)
            {
                return ServiceResult<PaginationResponse<LeadResponse>>.ValidationError("Page number must be greater than zero");
            }

            if (pageSize is < 1 or > 100)
            {
                return ServiceResult<PaginationResponse<LeadResponse>>.ValidationError("Page size must be between 1 and 100");
            }

            var skip = (long)(page - 1) * pageSize;

            if (skip > int.MaxValue)
            {
                return ServiceResult<PaginationResponse<LeadResponse>>.ValidationError("Requested page is too large");
            }

            // V1 business day is India Standard Time (UTC+05:30).
            // Today's appointments stay in the Today queue, even if their time has passed.
            var indiaOffset = TimeSpan.FromMinutes(330);
            var todayInIndia = DateTimeOffset.UtcNow.ToOffset(indiaOffset).Date;
            var startUtc = new DateTimeOffset(todayInIndia, indiaOffset).UtcDateTime;
            var canViewAll = string.Equals(access.Role, "Owner", StringComparison.OrdinalIgnoreCase) || string.Equals(access.Role, "Admin", StringComparison.OrdinalIgnoreCase);

            // Read the current appointment from Leads, not older follow-up records.
            var query = _dbContext.Leads.AsNoTracking().Where(lead => !lead.IsDeleted
                && lead.CompanyId == companyId
                && lead.Status != "Won"
                && lead.Status != "Lost"
                && lead.NextFollowUpDate.HasValue
                && lead.NextFollowUpDate.Value < startUtc
                && (canViewAll || lead.AssignedToUserId == requestedByUserId));

            var totalCount = await query.CountAsync(cancellationToken);

            var rows = await query.OrderBy(lead => lead.NextFollowUpDate)
                .ThenBy(lead => lead.Id)
                .Skip((int)skip)
                .Take(pageSize)
                .Select(lead => new LeadResponse
                {
                    LeadId = lead.Id,
                    CompanyId = lead.CompanyId,
                    LeadName = lead.LeadName,
                    Mobile = lead.Mobile,
                    Email = lead.Email,
                    CourseId = lead.CourseId,
                    CourseName = lead.Course != null ? lead.Course.CourseName : null,
                    SourceId = lead.SourceId,
                    SourceName = lead.Source != null ? lead.Source.SourceName : null,
                    AssignedToUserId = lead.AssignedToUserId,
                    AssignedToName = lead.AssignedToUser != null ? lead.AssignedToUser.Name : null,
                    Status = lead.Status,
                    Priority = lead.Priority,
                    NextFollowUpDate = lead.NextFollowUpDate,
                    LastFollowUpDate = lead.LastFollowUpDate,
                    LostReason = lead.LostReason,
                    CreatedOn = lead.CreatedOn,
                    UpdatedOn = lead.UpdatedOn
                }).ToListAsync(cancellationToken);

            var response = new PaginationResponse<LeadResponse>
            {
                Rows = rows,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return ServiceResult<PaginationResponse<LeadResponse>>.Ok(response, "Overdue follow-ups retrieved successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error retrieving overdue follow-ups for company {CompanyId}", companyId);
            return ServiceResult<PaginationResponse<LeadResponse>>.Error("An unexpected error occurred while retrieving overdue follow-ups");
        }
    }

    public async Task<ServiceResult<PaginationResponse<LeadResponse>>> GetUpcomingFollowUpsAsync(Guid companyId, Guid requestedByUserId, GetUpcomingFollowUpsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, ["Owner", "Admin", "Counsellor", "Staff"], cancellationToken);

            if (access.Status == CompanyAccessStatus.CompanyNotFound)
            {
                return ServiceResult<PaginationResponse<LeadResponse>>.NotFound("Company was not found");
            }

            if (access.Status != CompanyAccessStatus.Granted)
            {
                return ServiceResult<PaginationResponse<LeadResponse>>.Forbidden("You cannot view follow-ups in this company");
            }

            var page = request.PageNumber;
            var pageSize = request.PageSize;

            if (page < 1)
            {
                return ServiceResult<PaginationResponse<LeadResponse>>.ValidationError("Page number must be greater than zero");
            }

            if (pageSize is < 1 or > 100)
            {
                return ServiceResult<PaginationResponse<LeadResponse>>.ValidationError("Page size must be between 1 and 100");
            }

            var skip = (long)(page - 1) * pageSize;

            if (skip > int.MaxValue)
            {
                return ServiceResult<PaginationResponse<LeadResponse>>.ValidationError("Requested page is too large");
            }

            // V1 business day is India Standard Time (UTC+05:30).
            // Upcoming starts at tomorrow's midnight in IST; today stays in its own queue.
            var indiaOffset = TimeSpan.FromMinutes(330);
            var todayInIndia = DateTimeOffset.UtcNow.ToOffset(indiaOffset).Date;
            var tomorrowUtc = new DateTimeOffset(todayInIndia.AddDays(1), indiaOffset).UtcDateTime;
            var canViewAll = string.Equals(access.Role, "Owner", StringComparison.OrdinalIgnoreCase) || string.Equals(access.Role, "Admin", StringComparison.OrdinalIgnoreCase);

            // Read the current appointment from Leads, not older follow-up records.
            var query = _dbContext.Leads.AsNoTracking().Where(lead => !lead.IsDeleted
                && lead.CompanyId == companyId
                && lead.Status != "Won"
                && lead.Status != "Lost"
                && lead.NextFollowUpDate.HasValue
                && lead.NextFollowUpDate.Value >= tomorrowUtc
                && (canViewAll || lead.AssignedToUserId == requestedByUserId));

            var totalCount = await query.CountAsync(cancellationToken);

            var rows = await query.OrderBy(lead => lead.NextFollowUpDate)
                .ThenBy(lead => lead.Id)
                .Skip((int)skip)
                .Take(pageSize)
                .Select(lead => new LeadResponse
                {
                    LeadId = lead.Id,
                    CompanyId = lead.CompanyId,
                    LeadName = lead.LeadName,
                    Mobile = lead.Mobile,
                    Email = lead.Email,
                    CourseId = lead.CourseId,
                    CourseName = lead.Course != null ? lead.Course.CourseName : null,
                    SourceId = lead.SourceId,
                    SourceName = lead.Source != null ? lead.Source.SourceName : null,
                    AssignedToUserId = lead.AssignedToUserId,
                    AssignedToName = lead.AssignedToUser != null ? lead.AssignedToUser.Name : null,
                    Status = lead.Status,
                    Priority = lead.Priority,
                    NextFollowUpDate = lead.NextFollowUpDate,
                    LastFollowUpDate = lead.LastFollowUpDate,
                    LostReason = lead.LostReason,
                    CreatedOn = lead.CreatedOn,
                    UpdatedOn = lead.UpdatedOn
                }).ToListAsync(cancellationToken);

            var response = new PaginationResponse<LeadResponse>
            {
                Rows = rows,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return ServiceResult<PaginationResponse<LeadResponse>>.Ok(response, "Upcoming follow-ups retrieved successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error retrieving upcoming follow-ups for company {CompanyId}", companyId);
            return ServiceResult<PaginationResponse<LeadResponse>>.Error("An unexpected error occurred while retrieving upcoming follow-ups");
        }
    }

    public async Task<ServiceResult<PaginationResponse<LeadResponse>>> GetTodayFollowUpsAsync(Guid companyId, Guid requestedByUserId, GetTodayFollowUpsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, ["Owner", "Admin", "Counsellor", "Staff"], cancellationToken);

            if (access.Status == CompanyAccessStatus.CompanyNotFound)
            {
                return ServiceResult<PaginationResponse<LeadResponse>>.NotFound("Company was not found");
            }

            if (access.Status != CompanyAccessStatus.Granted)
            {
                return ServiceResult<PaginationResponse<LeadResponse>>.Forbidden("You cannot view follow-ups in this company");
            }

            var page = request.PageNumber;
            var pageSize = request.PageSize;

            if (page < 1)
            {
                return ServiceResult<PaginationResponse<LeadResponse>>.ValidationError("Page number must be greater than zero");
            }

            if (pageSize is < 1 or > 100)
            {
                return ServiceResult<PaginationResponse<LeadResponse>>.ValidationError("Page size must be between 1 and 100");
            }

            var skip = (long)(page - 1) * pageSize;

            if (skip > int.MaxValue)
            {
                return ServiceResult<PaginationResponse<LeadResponse>>.ValidationError("Requested page is too large");
            }

            // V1 business day is India Standard Time (UTC+05:30).
            // Compare UTC timestamps using [start of today, start of tomorrow).
            var indiaOffset = TimeSpan.FromMinutes(330);
            var todayInIndia = DateTimeOffset.UtcNow.ToOffset(indiaOffset).Date;
            var startUtc = new DateTimeOffset(todayInIndia, indiaOffset).UtcDateTime;
            var endUtc = startUtc.AddDays(1);
            var canViewAll = string.Equals(access.Role, "Owner", StringComparison.OrdinalIgnoreCase) || string.Equals(access.Role, "Admin", StringComparison.OrdinalIgnoreCase);

            // Read the current appointment from Leads, not older follow-up records.
            var query = _dbContext.Leads.AsNoTracking().Where(lead => !lead.IsDeleted
                && lead.CompanyId == companyId
                && lead.Status != "Won"
                && lead.Status != "Lost"
                && lead.NextFollowUpDate.HasValue
                && lead.NextFollowUpDate.Value >= startUtc
                && lead.NextFollowUpDate.Value < endUtc
                && (canViewAll || lead.AssignedToUserId == requestedByUserId));

            var totalCount = await query.CountAsync(cancellationToken);

            var rows = await query.OrderBy(lead => lead.NextFollowUpDate)
                .ThenBy(lead => lead.Id)
                .Skip((int)skip)
                .Take(pageSize)
                .Select(lead => new LeadResponse
                {
                    LeadId = lead.Id,
                    CompanyId = lead.CompanyId,
                    LeadName = lead.LeadName,
                    Mobile = lead.Mobile,
                    Email = lead.Email,
                    CourseId = lead.CourseId,
                    CourseName = lead.Course != null ? lead.Course.CourseName : null,
                    SourceId = lead.SourceId,
                    SourceName = lead.Source != null ? lead.Source.SourceName : null,
                    AssignedToUserId = lead.AssignedToUserId,
                    AssignedToName = lead.AssignedToUser != null ? lead.AssignedToUser.Name : null,
                    Status = lead.Status,
                    Priority = lead.Priority,
                    NextFollowUpDate = lead.NextFollowUpDate,
                    LastFollowUpDate = lead.LastFollowUpDate,
                    LostReason = lead.LostReason,
                    CreatedOn = lead.CreatedOn,
                    UpdatedOn = lead.UpdatedOn
                }).ToListAsync(cancellationToken);

            var response = new PaginationResponse<LeadResponse>
            {
                Rows = rows,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return ServiceResult<PaginationResponse<LeadResponse>>.Ok(response, "Today's follow-ups retrieved successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error retrieving today's follow-ups for company {CompanyId}", companyId);
            return ServiceResult<PaginationResponse<LeadResponse>>.Error("An unexpected error occurred while retrieving today's follow-ups");
        }
    }

}

