using FollowUpApi.Common;
using FollowUpApi.Features.CompanyUsers;

namespace FollowUpApi.Domain.Interfaces;

public interface IUserManager
{
    public Task<ServiceResult<AddCompanyUserResponse>> AddCompanyUserAsync(Guid companyId, Guid requestedByUserId, AddCompanyUserRequest request, CancellationToken cancellationToken = default);

    public Task<ServiceResult<PaginationResponse<CompanyUserListItemResponse>>> GetCompanyUsersAsync(Guid companyId, Guid requestedByUserId, GetCompanyUsersRequest request, CancellationToken cancellationToken = default);

    public Task<ServiceResult<CompanyUserListItemResponse>> UpdateCompanyUserRoleAsync(Guid companyId, Guid targetUserId, Guid requestedByUserId, UpdateCompanyUserRoleRequest request, CancellationToken cancellationToken = default);

    public Task<ServiceResult<CompanyUserListItemResponse>> DeactivateCompanyUserAsync(Guid companyId, Guid targetUserId, Guid requestedByUserId, CancellationToken cancellationToken = default);
}