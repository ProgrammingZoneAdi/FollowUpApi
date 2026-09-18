using FollowUpApi.Common;
using FollowUpApi.Features.LeadSources;

namespace FollowUpApi.Domain.Interfaces;

public interface ILeadSourceManager
{
    public Task<ServiceResult<LeadSourceResponse>> CreateLeadSourceAsync(Guid companyId, Guid requestedByUserId, CreateLeadSourceRequest request, CancellationToken cancellationToken = default);

    public Task<ServiceResult<PaginationResponse<LeadSourceResponse>>> GetLeadSourcesAsync(Guid companyId, Guid requestedByUserId, GetLeadSourcesRequest request, CancellationToken cancellationToken = default);

    public Task<ServiceResult<LeadSourceResponse>> UpdateLeadSourceAsync(Guid companyId, Guid sourceId, Guid requestedByUserId, UpdateLeadSourceRequest request, CancellationToken cancellationToken = default);

    public Task<ServiceResult<LeadSourceResponse>> DeactivateLeadSourceAsync(Guid companyId, Guid sourceId, Guid requestedByUserId, CancellationToken cancellationToken = default);
}
