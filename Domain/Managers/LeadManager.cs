using FollowUpApi.Common;
using FollowUpApi.DataContext;
using FollowUpApi.DataContext.Entities;
using FollowUpApi.Domain.Interfaces;
using FollowUpApi.Features.Leads;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace FollowUpApi.Domain.Managers;

public sealed class LeadManager : ILeadManager
{
    private const string ActiveStatus = "Active";
    private const string NewStatus = "New";
    private const string NormalPriority = "Normal";
    private static readonly string[] AllowedPriorities = ["Low", "Normal", "High", "Urgent"];
    private static readonly string[] AllowedLeadStatuses = ["New", "Contacted", "Follow-up", "Qualified", "Won", "Lost"];
    private static readonly string[] LeadAssignmentRoles = ["Owner", "Admin"];
    
    private readonly AppDbContext _dbContext;
    private readonly ICompanyAccessService _companyAccessService;
    private readonly ILogger<LeadManager> _logger;
    public LeadManager(AppDbContext dbContext, ICompanyAccessService companyAccessService, ILogger<LeadManager> logger)
    {
        _dbContext = dbContext;
        _companyAccessService = companyAccessService;
        _logger = logger;
    }

    public async Task<ServiceResult<LeadResponse>> CreateLeadAsync(Guid companyId, Guid requestedByUserId, CreateLeadRequest request, CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, Array.Empty<string>(), cancellationToken);

        if(access.Status == CompanyAccessStatus.CompanyNotFound)
        {
            return ServiceResult<LeadResponse>.NotFound("Company not found");
        }

        if(access.Status == CompanyAccessStatus.Forbidden)
        {
            return ServiceResult<LeadResponse>.Forbidden("You are not an active member of this company");
        }

        var leadName = request.LeadName?.Trim() ?? string.Empty;
        var mobile = NormalizeMobile(request.Mobile);
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(leadName))
        {
            return ServiceResult<LeadResponse>.ValidationError("Lead name is required");
        }

        if(leadName.Length > 150)
        {
            return ServiceResult<LeadResponse>.ValidationError("Lead name cannot exceed 150 characters");
        }

        if(mobile.Length < 10 || mobile.Length > 15)
        {
            return ServiceResult<LeadResponse>.ValidationError("Mobile number must contain between 10 and 15 digits");
        }

        if(email.Length > 254)
        {
            return ServiceResult<LeadResponse>.ValidationError("Email cannot exceed 254 characters");
        }

        if(!string.IsNullOrWhiteSpace(email) && (!MailAddress.TryCreate(email, out var parsedEmail) || !string.Equals(parsedEmail.Address, email, StringComparison.OrdinalIgnoreCase)))
        {
            return ServiceResult<LeadResponse>.ValidationError("Enter a valid email address");
        }

        var requestedPriority = request.Priority?.Trim();

        var priority = string.IsNullOrWhiteSpace(requestedPriority) ? NormalPriority : AllowedPriorities.FirstOrDefault(value => string.Equals(value, requestedPriority, StringComparison.OrdinalIgnoreCase));

        if(priority is null)
        {
            return ServiceResult<LeadResponse>.ValidationError("Priority must be Low, Normal, High, or Urgent");
        }

        var canAssignOthers = string.Equals(access.Role, "Owner", StringComparison.OrdinalIgnoreCase) || string.Equals(access.Role, "Admin", StringComparison.OrdinalIgnoreCase);

        var assignedToUserId = request.AssignedToUserId;

        // Counsellor/Staff can create leads only for themselves.

        if (!canAssignOthers)
        {
            if(assignedToUserId.HasValue && assignedToUserId.Value != requestedByUserId)
            {
                return ServiceResult<LeadResponse>.Forbidden("Only an owner or admin can assign a lead to another user");
            }

            assignedToUserId = requestedByUserId;
        }

        try
        {
            var duplicateMobileExists = await _dbContext.Leads.AsNoTracking().AnyAsync(lead => !lead.IsDeleted && lead.CompanyId == companyId && lead.Mobile == mobile, cancellationToken);

            if (duplicateMobileExists)
            {
                return ServiceResult<LeadResponse>.Conflict("A lead with the same mobile number already exists in this company");
            }

            string? courseName = null;

            if (request.CourseId.HasValue)
            {
                courseName = await _dbContext.Courses.AsNoTracking().Where(course => !course.IsDeleted && course.CompanyId == companyId && course.Id == request.CourseId.Value && course.Status == ActiveStatus)
                    .Select(course => course.CourseName).FirstOrDefaultAsync(cancellationToken);

                if(courseName is null)
                {
                    return ServiceResult<LeadResponse>.ValidationError("Selected course is invalid or inactive");
                }
            }

            string? sourceName = null;

            if (request.SourceId.HasValue)
            {
                sourceName = await _dbContext.LeadSources.AsNoTracking().Where(source => !source.IsDeleted && source.CompanyId == companyId && source.Id == request.SourceId.Value && source.Status == ActiveStatus)
                    .Select(source => source.SourceName).FirstOrDefaultAsync(cancellationToken);

                if(sourceName is null)
                {
                    return ServiceResult<LeadResponse>.ValidationError("Selected source is invalid or inactive");
                }
            }

            string? assignedToName = null;

            if (assignedToUserId.HasValue)
            {
                assignedToName = await _dbContext.LinkCompanyUsers.AsNoTracking().Where(member => !member.IsDeleted && member.CompanyId == companyId && member.UserId == assignedToUserId.Value
                && member.Status == ActiveStatus && member.User != null && !member.User.IsDeleted && member.User.Status == ActiveStatus)
                    .Select(member => member.User!.Name).FirstOrDefaultAsync(cancellationToken);

                if(assignedToName is null)
                {
                    return ServiceResult<LeadResponse>.ValidationError("Selected assignee is invalid or inactive");
                }
            }

            var lead = new Lead
            {
                CompanyId = companyId,
                LeadName = leadName,
                Mobile = mobile,
                Email = email,
                CourseId = request.CourseId,
                SourceId = request.SourceId,
                AssignedToUserId = assignedToUserId,
                Status = NewStatus,
                Priority = priority,
                NextFollowUpDate = request.NextFollowUpDate?.UtcDateTime,
                CreatedBy = requestedByUserId
                
            };

            var initialStatusHistory = new LeadStatusHistory
            {
                CompanyId = companyId,
                LeadId = lead.Id,
                OldStatus = string.Empty,
                NewStatus = NewStatus,
                Remarks = "Lead created",
                CreatedBy = requestedByUserId
            };

            _dbContext.Leads.Add(lead);
            _dbContext.LeadStatusHistories.Add(initialStatusHistory);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Lead {LeadId} created in company {CompanyId} by user {UserId}", lead.Id, companyId, requestedByUserId);

            return ServiceResult<LeadResponse>.Ok(MapLead(lead, courseName, sourceName, assignedToName), "Lead created successfully");
            
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch(DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Data conflict when creating a lead in company {CompanyId}", companyId);

            return ServiceResult<LeadResponse>.Conflict("Lead could not be created because the data conflicts with an existing record");
        }
        catch(Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while creating a lead in company {CompanyId}", companyId);
            return ServiceResult<LeadResponse>.Error("An unexpected error occured while creating the lead");
        }

    }

    
    public async Task<ServiceResult<PaginationResponse<LeadResponse>>> GetLeadsAsync(Guid companyId, Guid requestedByUserId, GetLeadsRequest request, CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, Array.Empty<string>(), cancellationToken);

        if(access.Status == CompanyAccessStatus.CompanyNotFound)
        {
            return ServiceResult<PaginationResponse<LeadResponse>>.NotFound("Company was not found");
        }

        if(access.Status == CompanyAccessStatus.Forbidden)
        {
            return ServiceResult<PaginationResponse<LeadResponse>>.Forbidden("You are not an active member of this company");
        }

        var pageNumber = request.PageNumber;
        var pageSize = request.PageSize;
        var search = request.Search?.Trim();
        var requestedStatus = request.Status?.Trim();
        var requestedPriority = request.Priority?.Trim();

        if(pageNumber < 1)
        {
            return ServiceResult<PaginationResponse<LeadResponse>>.ValidationError("Page number must be greater than zero");        
        }

        if(pageSize is < 1 or > 100)
        {
            return ServiceResult<PaginationResponse<LeadResponse>>.ValidationError("Page size must be between 1 and 100");
        }

        if(!string.IsNullOrWhiteSpace(search) && search.Length > 100)
        {
            return ServiceResult<PaginationResponse<LeadResponse>>.ValidationError("Search text cannot exceed 100 characters");
        }

        if((long)(pageNumber - 1) * pageSize > int.MaxValue)
        {
            return ServiceResult<PaginationResponse<LeadResponse>>.ValidationError("Requested page is too large");
        }

        string? statusFilter = null;
        
        if(!string.IsNullOrWhiteSpace(requestedStatus) && !string.Equals(requestedStatus, "All", StringComparison.OrdinalIgnoreCase))
        {
            statusFilter = AllowedLeadStatuses.FirstOrDefault(status => string.Equals(status, requestedStatus, StringComparison.OrdinalIgnoreCase));

            if(statusFilter is null)
            {
                return ServiceResult<PaginationResponse<LeadResponse>>.ValidationError("Status must be New, Contacted, Follow-up, " + "Qualified, Won, Lost, or All");
            }
        }

        string? priorityFilter = null;

        if(!string.IsNullOrWhiteSpace(requestedPriority) && !string.Equals(requestedPriority, "All", StringComparison.OrdinalIgnoreCase))
        {
            priorityFilter = AllowedPriorities.FirstOrDefault(priority => string.Equals(priority, requestedPriority, StringComparison.OrdinalIgnoreCase));

            if(priorityFilter is null)
            {
                return ServiceResult<PaginationResponse<LeadResponse>>.ValidationError("Priority must be Low, Normal, High, Urgent, or All");
            }
        }

        var canViewAllLeads = string.Equals(access.Role, "Owner", StringComparison.OrdinalIgnoreCase) || string.Equals(access.Role, "Admin", StringComparison.OrdinalIgnoreCase);

        if(!canViewAllLeads && request.AssignedToUserId.HasValue && request.AssignedToUserId.Value != requestedByUserId)
        {
            return ServiceResult<PaginationResponse<LeadResponse>>.Forbidden("You can only view leads assigned to you");
        }

        try
        {
            var query = _dbContext.Leads.AsNoTracking().Where(lead => !lead.IsDeleted && lead.CompanyId == companyId);

            if (!canViewAllLeads)
            {
                query = query.Where(lead => lead.AssignedToUserId == requestedByUserId);
            }

            if(statusFilter is not null)
            {
                query = query.Where(lead => lead.Status == statusFilter);
            }

            if(priorityFilter is not null)
            {
                query = query.Where(lead => lead.Priority == priorityFilter);
            }

            if (request.CourseId.HasValue)
            {
                query = query.Where(lead => lead.CourseId == request.CourseId.Value);
            }

            if (request.SourceId.HasValue)
            {
                query = query.Where(lead => lead.SourceId == request.SourceId.Value);
            }

            if (request.AssignedToUserId.HasValue)
            {
                query = query.Where(lead => lead.AssignedToUserId == request.AssignedToUserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchPattern = $"%{search}%";

                query = query.Where(lead => EF.Functions.ILike(lead.LeadName, searchPattern)
                   || EF.Functions.ILike(lead.Mobile, searchPattern)
                   || EF.Functions.ILike(lead.Email, searchPattern)
                   || (lead.Course != null && EF.Functions.ILike(lead.Course.CourseName, searchPattern))
                   || (lead.Source != null && EF.Functions.ILike(lead.Source.SourceName, searchPattern))
                   || (lead.AssignedToUser != null && EF.Functions.ILike(lead.AssignedToUser.Name, searchPattern)));

            }

            var totalCount = await query.CountAsync(cancellationToken);

            var rows = await query.OrderBy(lead => lead.NextFollowUpDate == null)
                .ThenBy(lead => lead.NextFollowUpDate)
                .ThenByDescending(lead => lead.CreatedOn)
                .ThenBy(lead => lead.Id)
                .Skip((pageNumber - 1) * pageSize)
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
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ServiceResult<PaginationResponse<LeadResponse>>.Ok(response, "Leads retrieved successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch(Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while retrieving leads " + "for company {CompanyId}", companyId);

            return ServiceResult<PaginationResponse<LeadResponse>>.Error("An unexpected error occurred while retrieving leads");
        }
    }

    public async Task<ServiceResult<LeadResponse>> GetLeadDetailsAsync(Guid companyId, Guid leadId, Guid requestedByUserId, CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, Array.Empty<string>(), cancellationToken);

        if (access.Status == CompanyAccessStatus.CompanyNotFound)
        {
            return ServiceResult<LeadResponse>.NotFound("Company was not found");
        }

        if (access.Status == CompanyAccessStatus.Forbidden)
        {
            return ServiceResult<LeadResponse>.Forbidden("You are not an active member of this company");
        }

        var canViewAllLeads = string.Equals(access.Role, "Owner", StringComparison.OrdinalIgnoreCase) || string.Equals(access.Role, "Admin", StringComparison.OrdinalIgnoreCase);

        try
        {
            var query = _dbContext.Leads.AsNoTracking().Where(lead => !lead.IsDeleted && lead.CompanyId == companyId && lead.Id == leadId);

            if (!canViewAllLeads)
            {
                query = query.Where(lead => lead.AssignedToUserId == requestedByUserId);
            }

            var lead = await query.Select(lead => new LeadResponse
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
            }).FirstOrDefaultAsync(cancellationToken);

            if(lead is null)
            {
                return ServiceResult<LeadResponse>.NotFound("Lead was not found");
            }

            return ServiceResult<LeadResponse>.Ok(lead, "Lead retrieved successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch(Exception exception)
        {
            _logger.LogError(exception, "Unexpected error retrieving lead {LeadId} " + "for company {CompanyId}", leadId, companyId);

            return ServiceResult<LeadResponse>.Error("An unexpected error occurred while retrieving the lead");
        }

    }
    public async Task<ServiceResult<LeadResponse>> UpdateLeadAsync(
        Guid companyId,
        Guid leadId,
        Guid requestedByUserId,
        UpdateLeadRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.CheckAccessAsync(
            companyId, requestedByUserId, Array.Empty<string>(), cancellationToken);

        if (access.Status == CompanyAccessStatus.CompanyNotFound)
        {
            return ServiceResult<LeadResponse>.NotFound("Company was not found");
        }

        if (access.Status == CompanyAccessStatus.Forbidden)
        {
            return ServiceResult<LeadResponse>.Forbidden(
                "You are not an active member of this company");
        }

        var leadName = request.LeadName?.Trim() ?? string.Empty;
        var mobile = NormalizeMobile(request.Mobile);
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var requestedPriority = request.Priority?.Trim();

        if (string.IsNullOrWhiteSpace(leadName))
        {
            return ServiceResult<LeadResponse>.ValidationError("Lead name is required");
        }

        if (leadName.Length > 150)
        {
            return ServiceResult<LeadResponse>.ValidationError(
                "Lead name cannot exceed 150 characters");
        }

        if (mobile.Length is < 10 or > 15)
        {
            return ServiceResult<LeadResponse>.ValidationError(
                "Mobile number must contain between 10 and 15 digits");
        }

        if (email.Length > 254)
        {
            return ServiceResult<LeadResponse>.ValidationError(
                "Email cannot exceed 254 characters");
        }

        if (!string.IsNullOrWhiteSpace(email)
            && (!MailAddress.TryCreate(email, out var parsedEmail)
                || !string.Equals(parsedEmail.Address, email,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return ServiceResult<LeadResponse>.ValidationError(
                "Enter a valid email address");
        }

        var priority = string.IsNullOrWhiteSpace(requestedPriority)
            ? NormalPriority
            : AllowedPriorities.FirstOrDefault(value =>
                string.Equals(value, requestedPriority,
                    StringComparison.OrdinalIgnoreCase));

        if (priority is null)
        {
            return ServiceResult<LeadResponse>.ValidationError(
                "Priority must be Low, Normal, High, or Urgent");
        }

        var canEditAllLeads =
            string.Equals(access.Role, "Owner", StringComparison.OrdinalIgnoreCase)
            || string.Equals(access.Role, "Admin", StringComparison.OrdinalIgnoreCase);

        try
        {
            var lead = await _dbContext.Leads
                .Include(item => item.AssignedToUser)
                .FirstOrDefaultAsync(item =>
                    !item.IsDeleted
                    && item.CompanyId == companyId
                    && item.Id == leadId,
                    cancellationToken);

            if (lead is null
                || (!canEditAllLeads
                    && lead.AssignedToUserId != requestedByUserId))
            {
                return ServiceResult<LeadResponse>.NotFound("Lead was not found");
            }

            var duplicateMobileExists = await _dbContext.Leads
                .AsNoTracking()
                .AnyAsync(item =>
                    !item.IsDeleted
                    && item.CompanyId == companyId
                    && item.Id != leadId
                    && item.Mobile == mobile,
                    cancellationToken);

            if (duplicateMobileExists)
            {
                return ServiceResult<LeadResponse>.Conflict(
                    "A lead with the same mobile number already exists in this company");
            }

            string? courseName = null;

            if (request.CourseId.HasValue)
            {
                courseName = await _dbContext.Courses
                    .AsNoTracking()
                    .Where(course =>
                        !course.IsDeleted
                        && course.CompanyId == companyId
                        && course.Id == request.CourseId.Value
                        && (course.Status == ActiveStatus
                            || course.Id == lead.CourseId))
                    .Select(course => course.CourseName)
                    .FirstOrDefaultAsync(cancellationToken);

                if (courseName is null)
                {
                    return ServiceResult<LeadResponse>.ValidationError(
                        "Selected course is invalid or inactive");
                }
            }

            string? sourceName = null;

            if (request.SourceId.HasValue)
            {
                sourceName = await _dbContext.LeadSources
                    .AsNoTracking()
                    .Where(source =>
                        !source.IsDeleted
                        && source.CompanyId == companyId
                        && source.Id == request.SourceId.Value
                        && (source.Status == ActiveStatus
                            || source.Id == lead.SourceId))
                    .Select(source => source.SourceName)
                    .FirstOrDefaultAsync(cancellationToken);

                if (sourceName is null)
                {
                    return ServiceResult<LeadResponse>.ValidationError(
                        "Selected source is invalid or inactive");
                }
            }

            lead.LeadName = leadName;
            lead.Mobile = mobile;
            lead.Email = email;
            lead.CourseId = request.CourseId;
            lead.SourceId = request.SourceId;
            lead.Priority = priority;
            lead.UpdatedOn = DateTime.UtcNow;
            lead.UpdatedBy = requestedByUserId;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Lead {LeadId} updated in company {CompanyId} by user {UserId}",
                leadId, companyId, requestedByUserId);

            return ServiceResult<LeadResponse>.Ok(
                MapLead(lead, courseName, sourceName, lead.AssignedToUser?.Name),
                "Lead updated successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(exception,
                "Database conflict while updating lead {LeadId} in company {CompanyId}",
                leadId, companyId);

            return ServiceResult<LeadResponse>.Conflict(
                "Lead could not be updated because the data conflicts with an existing record");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception,
                "Unexpected error updating lead {LeadId} in company {CompanyId}",
                leadId, companyId);

            return ServiceResult<LeadResponse>.Error(
                "An unexpected error occurred while updating the lead");
        }
    }

    public async Task<ServiceResult<LeadResponse>> AssignLeadAsync(
        Guid companyId,
        Guid leadId,
        Guid requestedByUserId,
        AssignLeadRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.CheckAccessAsync(
            companyId, requestedByUserId, LeadAssignmentRoles, cancellationToken);

        if (access.Status == CompanyAccessStatus.CompanyNotFound)
        {
            return ServiceResult<LeadResponse>.NotFound("Company was not found");
        }

        if (access.Status == CompanyAccessStatus.Forbidden)
        {
            return ServiceResult<LeadResponse>.Forbidden(
                "Only a company owner or admin can assign leads");
        }

        if (request.AssignedToUserId == Guid.Empty)
        {
            return ServiceResult<LeadResponse>.ValidationError(
                "Assigned user ID cannot be empty");
        }

        try
        {
            var lead = await _dbContext.Leads
                .Include(item => item.Course)
                .Include(item => item.Source)
                .FirstOrDefaultAsync(item =>
                    !item.IsDeleted
                    && item.CompanyId == companyId
                    && item.Id == leadId,
                    cancellationToken);

            if (lead is null)
            {
                return ServiceResult<LeadResponse>.NotFound("Lead was not found");
            }

            string? assignedToName = null;

            if (request.AssignedToUserId.HasValue)
            {
                assignedToName = await _dbContext.LinkCompanyUsers
                    .AsNoTracking()
                    .Where(member =>
                        !member.IsDeleted
                        && member.CompanyId == companyId
                        && member.UserId == request.AssignedToUserId.Value
                        && member.Status == ActiveStatus
                        && member.User != null
                        && !member.User.IsDeleted
                        && member.User.Status == ActiveStatus)
                    .Select(member => member.User!.Name)
                    .FirstOrDefaultAsync(cancellationToken);

                if (assignedToName is null)
                {
                    return ServiceResult<LeadResponse>.ValidationError(
                        "Selected assignee is invalid or inactive");
                }
            }

            if (lead.AssignedToUserId == request.AssignedToUserId)
            {
                return ServiceResult<LeadResponse>.Ok(
                    MapLead(lead, lead.Course?.CourseName,
                        lead.Source?.SourceName, assignedToName),
                    "Lead assignment is already up to date");
            }

            var previousAssigneeId = lead.AssignedToUserId;
            lead.AssignedToUserId = request.AssignedToUserId;
            lead.UpdatedOn = DateTime.UtcNow;
            lead.UpdatedBy = requestedByUserId;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Lead {LeadId} assignment changed from {PreviousAssigneeId} to {AssigneeId} in company {CompanyId} by user {UserId}",
                leadId, previousAssigneeId, request.AssignedToUserId,
                companyId, requestedByUserId);

            return ServiceResult<LeadResponse>.Ok(
                MapLead(lead, lead.Course?.CourseName,
                    lead.Source?.SourceName, assignedToName),
                request.AssignedToUserId.HasValue
                    ? "Lead assigned successfully"
                    : "Lead unassigned successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(exception,
                "Database conflict while assigning lead {LeadId} in company {CompanyId}",
                leadId, companyId);

            return ServiceResult<LeadResponse>.Conflict(
                "Lead assignment could not be saved because the data conflicts with an existing record");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception,
                "Unexpected error while assigning lead {LeadId} in company {CompanyId}",
                leadId, companyId);

            return ServiceResult<LeadResponse>.Error(
                "An unexpected error occurred while assigning the lead");
        }
    }

    public async Task<ServiceResult<LeadResponse>> ChangeLeadStatusAsync(
        Guid companyId,
        Guid leadId,
        Guid requestedByUserId,
        ChangeLeadStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await _companyAccessService.CheckAccessAsync(
                companyId, requestedByUserId,
                ["Owner", "Admin", "Counsellor", "Staff"], cancellationToken);

            if (access.Status == CompanyAccessStatus.CompanyNotFound)
                return ServiceResult<LeadResponse>.NotFound("Company was not found");

            if (access.Status != CompanyAccessStatus.Granted)
                return ServiceResult<LeadResponse>.Forbidden(
                    "You do not have permission to change lead status in this company");

            var status = AllowedLeadStatuses.FirstOrDefault(value =>
                string.Equals(value, request.Status?.Trim(), StringComparison.OrdinalIgnoreCase));
            var lostReason = request.LostReason?.Trim() ?? string.Empty;
            var remarks = request.Remarks?.Trim() ?? string.Empty;

            if (status is null)
                return ServiceResult<LeadResponse>.ValidationError(
                    "Status must be New, Contacted, Follow-up, Qualified, Won, or Lost");

            if (status == "Lost" && string.IsNullOrWhiteSpace(lostReason))
                return ServiceResult<LeadResponse>.ValidationError(
                    "Lost reason is required when status is Lost");

            if (lostReason.Length > 1000 || remarks.Length > 2000)
                return ServiceResult<LeadResponse>.ValidationError(
                    "Lost reason cannot exceed 1000 characters and remarks cannot exceed 2000 characters");

            if (status != "Lost" && lostReason.Length > 0)
                return ServiceResult<LeadResponse>.ValidationError(
                    "Lost reason can only be supplied when status is Lost");

            // Serialize competing status changes so each history entry has the correct old status.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);

            var canManageAll = string.Equals(access.Role, "Owner", StringComparison.OrdinalIgnoreCase)
                || string.Equals(access.Role, "Admin", StringComparison.OrdinalIgnoreCase);

            var lead = await _dbContext.Leads
                .Include(item => item.Course)
                .Include(item => item.Source)
                .Include(item => item.AssignedToUser)
                .FirstOrDefaultAsync(item => !item.IsDeleted
                    && item.CompanyId == companyId && item.Id == leadId
                    && (canManageAll || item.AssignedToUserId == requestedByUserId), cancellationToken);

            if (lead is null)
                return ServiceResult<LeadResponse>.NotFound("Lead was not found");

            if (lead.Status == status && lead.LostReason == lostReason)
            {
                return ServiceResult<LeadResponse>.Ok(
                    MapLead(lead, lead.Course?.CourseName, lead.Source?.SourceName,
                        lead.AssignedToUser?.Name), "Lead status is already up to date");
            }

            var oldStatus = lead.Status;
            var now = DateTime.UtcNow;
            lead.Status = status;
            lead.LostReason = lostReason;
            lead.UpdatedOn = now;
            lead.UpdatedBy = requestedByUserId;

            if (status is "Won" or "Lost")
                lead.NextFollowUpDate = null;

            // Preserve the lost reason in history even if the lead is reopened later.
            var historyRemarks = status == "Lost"
                ? $"Lost reason: {lostReason}" + (remarks.Length > 0 ? $"\n{remarks}" : string.Empty)
                : remarks;

            _dbContext.LeadStatusHistories.Add(new LeadStatusHistory
            {
                CompanyId = companyId,
                LeadId = lead.Id,
                OldStatus = oldStatus,
                NewStatus = status,
                Remarks = historyRemarks,
                CreatedOn = now,
                CreatedBy = requestedByUserId
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Lead {LeadId} status changed from {OldStatus} to {NewStatus} in company {CompanyId} by {UserId}",
                leadId, oldStatus, status, companyId, requestedByUserId);

            return ServiceResult<LeadResponse>.Ok(
                MapLead(lead, lead.Course?.CourseName, lead.Source?.SourceName,
                    lead.AssignedToUser?.Name), "Lead status updated successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Npgsql.PostgresException exception) when (exception.SqlState is "40001" or "40P01")
        {
            _logger.LogWarning(exception, "Concurrent status change for lead {LeadId}", leadId);
            return ServiceResult<LeadResponse>.Conflict(
                "Lead changed while saving. Reload its details and retry");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Database conflict changing status of lead {LeadId}", leadId);
            return ServiceResult<LeadResponse>.Conflict(
                "Lead status could not be saved. Reload its details and retry");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error changing status of lead {LeadId} in company {CompanyId}",
                leadId, companyId);
            return ServiceResult<LeadResponse>.Error(
                "An unexpected error occurred while changing lead status");
        }
    }

    public async Task<ServiceResult<DeactivateLeadResponse>> DeactivateLeadAsync(
        Guid companyId,
        Guid leadId,
        Guid requestedByUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await _companyAccessService.CheckAccessAsync(
                companyId, requestedByUserId, ["Owner", "Admin"], cancellationToken);

            if (access.Status == CompanyAccessStatus.CompanyNotFound)
                return ServiceResult<DeactivateLeadResponse>.NotFound("Company was not found");

            if (access.Status != CompanyAccessStatus.Granted)
                return ServiceResult<DeactivateLeadResponse>.Forbidden(
                    "Only a company owner or admin can deactivate leads");

            // A conditional update also makes simultaneous repeated deletes harmless.
            // Preserve status, scheduling data and related history for future recovery.
            var now = DateTime.UtcNow;
            var affectedRows = await _dbContext.Leads
                .Where(lead => lead.CompanyId == companyId
                    && lead.Id == leadId && !lead.IsDeleted)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(lead => lead.IsDeleted, true)
                    .SetProperty(lead => lead.UpdatedOn, (DateTime?)now)
                    .SetProperty(lead => lead.UpdatedBy, (Guid?)requestedByUserId),
                    cancellationToken);

            // Include already-deactivated leads so retrying DELETE returns success.
            var response = await _dbContext.Leads.AsNoTracking()
                .Where(lead => lead.CompanyId == companyId && lead.Id == leadId)
                .Select(lead => new DeactivateLeadResponse
                {
                    LeadId = lead.Id,
                    CompanyId = lead.CompanyId,
                    IsDeleted = lead.IsDeleted,
                    UpdatedOn = lead.UpdatedOn
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (response is null)
                return ServiceResult<DeactivateLeadResponse>.NotFound("Lead was not found");

            if (affectedRows > 0)
            {
                _logger.LogInformation(
                    "Lead {LeadId} deactivated in company {CompanyId} by {UserId}",
                    leadId, companyId, requestedByUserId);
            }

            return ServiceResult<DeactivateLeadResponse>.Ok(response,
                affectedRows > 0 ? "Lead deactivated successfully" : "Lead is already deactivated");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Npgsql.PostgresException exception) when (exception.SqlState is "40001" or "40P01")
        {
            _logger.LogWarning(exception, "Concurrent change while deactivating lead {LeadId}", leadId);
            return ServiceResult<DeactivateLeadResponse>.Conflict(
                "Lead changed while saving. Reload its details and retry");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error deactivating lead {LeadId} in company {CompanyId}",
                leadId, companyId);
            return ServiceResult<DeactivateLeadResponse>.Error(
                "An unexpected error occurred while deactivating the lead");
        }
    }

    private static string NormalizeMobile(string? mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile))
        {
            return string.Empty;
        }

        return new string(mobile.Where(char.IsDigit).ToArray());
    }

    private static LeadResponse MapLead(Lead lead, string? courseName, string? sourceName, string? assignedToName)
    {
        return new LeadResponse
        {
            LeadId = lead.Id,
            CompanyId = lead.CompanyId,
            LeadName = lead.LeadName,
            Mobile = lead.Mobile,
            Email = lead.Email,
            CourseId  = lead.CourseId,
            CourseName = courseName,
            SourceId = lead.SourceId,
            SourceName = sourceName,
            AssignedToUserId = lead.AssignedToUserId,
            AssignedToName = assignedToName,
            Status = lead.Status,
            Priority = lead.Priority,
            NextFollowUpDate = lead.NextFollowUpDate,
            LastFollowUpDate = lead.LastFollowUpDate,
            LostReason = lead.LostReason,
            CreatedOn = lead.CreatedOn,
            UpdatedOn = lead.UpdatedOn
        };
    }
}
