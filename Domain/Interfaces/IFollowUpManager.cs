namespace FollowUpApi.Domain.Interfaces;

using FollowUpApi.Common;
using FollowUpApi.Features.FollowUps;
using FollowUpApi.Features.Leads;

public interface IFollowUpManager
{
    Task<ServiceResult<FollowUpResponse>> AddFollowUpAsync(Guid companyId, Guid leadId, Guid requestedByUserId, AddFollowUpRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<PaginationResponse<FollowUpResponse>>> GetFollowUpsAsync(Guid companyId, Guid leadId, Guid requestedByUserId, GetFollowUpsRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<PaginationResponse<LeadResponse>>> GetTodayFollowUpsAsync(Guid companyId, Guid requestedByUserId, GetTodayFollowUpsRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<PaginationResponse<LeadResponse>>> GetOverdueFollowUpsAsync(Guid companyId, Guid requestedByUserId, GetOverdueFollowUpsRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<PaginationResponse<LeadResponse>>> GetUpcomingFollowUpsAsync(Guid companyId, Guid requestedByUserId, GetUpcomingFollowUpsRequest request, CancellationToken cancellationToken = default);
}
