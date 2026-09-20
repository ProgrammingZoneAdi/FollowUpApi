using FollowUpApi.DataContext;
using FollowUpApi.Domain.Interfaces;
using FollowUpApi.Common;
using FollowUpApi.Features.Dashboard;
using FollowUpApi.Features.Leads;
using Microsoft.EntityFrameworkCore;

namespace FollowUpApi.Domain.Managers;

public class DashboardManager : IDashboardManager
{
    private readonly AppDbContext _dbContext;
    private readonly ICompanyAccessService _companyAccessService;
    private readonly ILogger<DashboardManager> _logger;
    private static readonly string[] PipelineStatuses = ["New", "Contacted", "Follow-up", "Qualified", "Won", "Lost"];

    public DashboardManager(AppDbContext dbContext, ICompanyAccessService companyAccessService, ILogger<DashboardManager> logger)
    {
        _dbContext = dbContext;
        _companyAccessService = companyAccessService;
        _logger = logger;
    }

    public async Task<ServiceResult<DashboardSummaryResponse>> GetDashboardSummaryAsync(Guid companyId, Guid requestedByUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, ["Owner", "Admin", "Counsellor", "Staff"], cancellationToken);

            if (access.Status == CompanyAccessStatus.CompanyNotFound)
            {
                return ServiceResult<DashboardSummaryResponse>.NotFound("Company was not found");
            }

            if (access.Status != CompanyAccessStatus.Granted)
            {
                return ServiceResult<DashboardSummaryResponse>.Forbidden("You cannot view the dashboard for this company");
            }

            var now = DateTime.UtcNow;
            var indiaOffset = TimeSpan.FromMinutes(330);
            var todayInIndia = new DateTimeOffset(now).ToOffset(indiaOffset).Date;
            var startUtc = new DateTimeOffset(todayInIndia, indiaOffset).UtcDateTime;
            var endUtc = startUtc.AddDays(1);
            var canViewAll = string.Equals(access.Role, "Owner", StringComparison.OrdinalIgnoreCase) || string.Equals(access.Role, "Admin", StringComparison.OrdinalIgnoreCase);

            // All dashboard queries use one database snapshot so cards and pipeline agree.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);

            var leads = _dbContext.Leads.AsNoTracking().Where(lead => !lead.IsDeleted && lead.CompanyId == companyId
                && (canViewAll || lead.AssignedToUserId == requestedByUserId));

            var statusCounts = await leads.GroupBy(lead => lead.Status)
                .Select(group => new DashboardStatusResponse
                {
                    Status = group.Key,
                    Count = group.Count()
                }).ToListAsync(cancellationToken);

            var openLeads = leads.Where(lead => lead.Status != "Won" && lead.Status != "Lost");

            var appointments = await openLeads.GroupBy(lead => 1)
                .Select(group => new
                {
                    Today = group.Count(lead => lead.NextFollowUpDate.HasValue && lead.NextFollowUpDate.Value >= startUtc && lead.NextFollowUpDate.Value < endUtc),
                    Overdue = group.Count(lead => lead.NextFollowUpDate.HasValue && lead.NextFollowUpDate.Value < startUtc),
                    Upcoming = group.Count(lead => lead.NextFollowUpDate.HasValue && lead.NextFollowUpDate.Value >= endUtc),
                    Unscheduled = group.Count(lead => !lead.NextFollowUpDate.HasValue)
                }).FirstOrDefaultAsync(cancellationToken);

            var nextFollowUps = await openLeads.Where(lead => lead.NextFollowUpDate.HasValue && lead.NextFollowUpDate.Value >= now && lead.NextFollowUpDate.Value < endUtc)
                .OrderBy(lead => lead.NextFollowUpDate)
                .ThenBy(lead => lead.Id)
                .Take(5)
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

            await transaction.CommitAsync(cancellationToken);

            var totalLeads = statusCounts.Sum(item => item.Count);
            var convertedLeads = statusCounts.FirstOrDefault(item => item.Status == "Won")?.Count ?? 0;
            var lostLeads = statusCounts.FirstOrDefault(item => item.Status == "Lost")?.Count ?? 0;

            var response = new DashboardSummaryResponse
            {
                CompanyId = companyId,
                SummaryDate = DateOnly.FromDateTime(todayInIndia),
                TimeZone = "Asia/Kolkata",
                Scope = canViewAll ? "Company" : "Assigned",
                GeneratedAt = now,
                TotalLeads = totalLeads,
                ActiveLeads = totalLeads - convertedLeads - lostLeads,
                ConvertedLeads = convertedLeads,
                ConversionRate = totalLeads == 0 ? 0m : Math.Round(convertedLeads * 100m / totalLeads, 2),
                TodayFollowUps = appointments?.Today ?? 0,
                OverdueFollowUps = appointments?.Overdue ?? 0,
                UpcomingFollowUps = appointments?.Upcoming ?? 0,
                UnscheduledLeads = appointments?.Unscheduled ?? 0,
                Pipeline = PipelineStatuses.Select(status => new DashboardStatusResponse
                {
                    Status = status,
                    Count = statusCounts.FirstOrDefault(item => item.Status == status)?.Count ?? 0
                }).ToList(),
                NextFollowUpsToday = nextFollowUps
            };

            return ServiceResult<DashboardSummaryResponse>.Ok(response, "Dashboard summary retrieved successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error retrieving dashboard summary for company {CompanyId}", companyId);
            return ServiceResult<DashboardSummaryResponse>.Error("An unexpected error occurred while retrieving dashboard summary");
        }
    }
}
