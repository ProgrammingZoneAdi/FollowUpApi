using FollowUpApi.Common;
using FollowUpApi.Features.Leads;

namespace FollowUpApi.Domain.Interfaces;

public interface ILeadManager
{
    Task<ServiceResult<DeactivateLeadResponse>> DeactivateLeadAsync(
        Guid companyId,
        Guid leadId,
        Guid requestedByUserId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<LeadResponse>> ChangeLeadStatusAsync(
        Guid companyId,
        Guid leadId,
        Guid requestedByUserId,
        ChangeLeadStatusRequest request,
        CancellationToken cancellationToken = default);

    public Task<ServiceResult<LeadResponse>> CreateLeadAsync(Guid companyId, Guid requestedByUserId, CreateLeadRequest request, CancellationToken cancellationToken = default);

    public Task<ServiceResult<PaginationResponse<LeadResponse>>> GetLeadsAsync(Guid companyId, Guid requestedByUserId, GetLeadsRequest request, CancellationToken cancellationToken = default);

    public Task<ServiceResult<LeadResponse>> GetLeadDetailsAsync(Guid companyId, Guid leadId, Guid requestedByUserId, CancellationToken cancellationToken = default);

    Task<ServiceResult<LeadResponse>> UpdateLeadAsync(
        Guid companyId,
        Guid leadId,
        Guid requestedByUserId,
        UpdateLeadRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<LeadResponse>> AssignLeadAsync(
        Guid companyId,
        Guid leadId,
        Guid requestedByUserId,
        AssignLeadRequest request,
        CancellationToken cancellationToken = default);
}
