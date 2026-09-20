namespace FollowUpApi.Domain.Interfaces;

using FollowUpApi.Common;
using FollowUpApi.Features.Dashboard;

public interface IDashboardManager
{
    Task<ServiceResult<DashboardSummaryResponse>> GetDashboardSummaryAsync(Guid companyId, Guid requestedByUserId, CancellationToken cancellationToken = default);
}
