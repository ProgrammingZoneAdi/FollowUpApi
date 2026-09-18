namespace FollowUpApi.Domain.Interfaces;

using FollowUpApi.Common;
using FollowUpApi.Features.FollowUps;

public interface IFollowUpManager
{
    Task<ServiceResult<FollowUpResponse>> AddFollowUpAsync(
        Guid companyId, Guid leadId, Guid requestedByUserId,
        AddFollowUpRequest request, CancellationToken cancellationToken = default);
}
