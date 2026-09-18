using FollowUpApi.DataContext;
using FollowUpApi.Domain.Interfaces;
using FollowUpApi.Common;
using FollowUpApi.DataContext.Entities;
using FollowUpApi.Features.FollowUps;
using Microsoft.EntityFrameworkCore;

namespace FollowUpApi.Domain.Managers;

public class FollowUpManager : IFollowUpManager
{
    private readonly AppDbContext _dbContext;
    private readonly ICompanyAccessService _companyAccessService;
    private readonly ILogger<FollowUpManager> _logger;
    private static readonly string[] AllowedTypes = ["Call", "WhatsApp", "Email", "Meeting", "SMS", "Other"];
    private static readonly string[] AllowedStatuses = ["New", "Contacted", "Follow-up", "Qualified", "Won", "Lost"];

    public FollowUpManager(AppDbContext dbContext, ICompanyAccessService companyAccessService,
        ILogger<FollowUpManager> logger)
    {
        _dbContext = dbContext;
        _companyAccessService = companyAccessService;
        _logger = logger;
    }

    public async Task<ServiceResult<FollowUpResponse>> AddFollowUpAsync(
        Guid companyId, Guid leadId, Guid requestedByUserId,
        AddFollowUpRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId,
                ["Owner", "Admin", "Counsellor", "Staff"], cancellationToken);
            if (access.Status == CompanyAccessStatus.CompanyNotFound)
                return ServiceResult<FollowUpResponse>.NotFound("Company was not found");
            if (access.Status != CompanyAccessStatus.Granted)
                return ServiceResult<FollowUpResponse>.Forbidden("You cannot add follow-ups in this company");

            var type = AllowedTypes.FirstOrDefault(value => string.Equals(value,
                request.FollowUpType?.Trim(), StringComparison.OrdinalIgnoreCase));
            var status = AllowedStatuses.FirstOrDefault(value => string.Equals(value,
                request.StatusAfterFollowUp?.Trim(), StringComparison.OrdinalIgnoreCase));
            var remark = request.Remark?.Trim() ?? string.Empty;
            var lostReason = request.LostReason?.Trim() ?? string.Empty;
            var nextDate = request.NextFollowUpDate?.UtcDateTime;

            if (type is null)
                return ServiceResult<FollowUpResponse>.ValidationError(
                    "Follow-up type must be Call, WhatsApp, Email, Meeting, SMS, or Other");
            if (status is null)
                return ServiceResult<FollowUpResponse>.ValidationError(
                    "Status must be New, Contacted, Follow-up, Qualified, Won, or Lost");
            if (string.IsNullOrWhiteSpace(remark) || remark.Length > 2000)
                return ServiceResult<FollowUpResponse>.ValidationError("Remark is required and cannot exceed 2000 characters");
            if (lostReason.Length > 1000)
                return ServiceResult<FollowUpResponse>.ValidationError("Lost reason cannot exceed 1000 characters");
            if (status == "Lost" && string.IsNullOrWhiteSpace(lostReason))
                return ServiceResult<FollowUpResponse>.ValidationError("Lost reason is required when status is Lost");
            if (status != "Lost" && lostReason.Length > 0)
                return ServiceResult<FollowUpResponse>.ValidationError("Lost reason can only be supplied for Lost status");
            if ((status is "Won" or "Lost") && nextDate.HasValue)
                return ServiceResult<FollowUpResponse>.ValidationError("Next follow-up date must be null for Won or Lost status");
            if (status == "Follow-up" && !nextDate.HasValue)
                return ServiceResult<FollowUpResponse>.ValidationError("Next follow-up date is required for Follow-up status");

            // Keep the interaction, lead summary and history consistent under concurrent requests.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);
            var canManageAll = string.Equals(access.Role, "Owner", StringComparison.OrdinalIgnoreCase)
                || string.Equals(access.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            var lead = await _dbContext.Leads.FirstOrDefaultAsync(item =>
                !item.IsDeleted && item.CompanyId == companyId && item.Id == leadId
                && (canManageAll || item.AssignedToUserId == requestedByUserId), cancellationToken);
            if (lead is null)
                return ServiceResult<FollowUpResponse>.NotFound("Lead was not found");
            if (lead.Status is "Won" or "Lost")
                return ServiceResult<FollowUpResponse>.Conflict("Reopen the lead using the status API before adding a follow-up");

            var now = DateTime.UtcNow;
            if (nextDate.HasValue && nextDate.Value <= now)
                return ServiceResult<FollowUpResponse>.ValidationError("Next follow-up date must be in the future");

            var followUp = new LeadFollowUp
            {
                CompanyId = companyId, LeadId = leadId,
                FollowUpType = type, Remark = remark,
                StatusAfterFollowUp = status, FollowUpDate = now,
                NextFollowUpDate = nextDate, CreatedOn = now, CreatedBy = requestedByUserId
            };
            if (lead.Status != status || lead.LostReason != lostReason)
            {
                _dbContext.LeadStatusHistories.Add(new LeadStatusHistory
                {
                    CompanyId = companyId, LeadId = leadId,
                    OldStatus = lead.Status, NewStatus = status,
                    Remarks = status == "Lost" ? $"Lost reason: {lostReason}\n{remark}" : remark,
                    CreatedOn = now, CreatedBy = requestedByUserId
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
                FollowUpId = followUp.Id, CompanyId = companyId, LeadId = leadId,
                FollowUpType = type, Remark = remark, StatusAfterFollowUp = status,
                FollowUpDate = now, NextFollowUpDate = nextDate,
                CreatedBy = requestedByUserId, CreatedOn = now
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
}

