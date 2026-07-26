using FollowUpApi.Common;
using FollowUpApi.Features.CompanyUsers;

namespace FollowUpApi.Domain.Interfaces;

public interface IUserManager
{
    public Task<ServiceResult<AddCompanyUserResponse>> AddCompanyUserAsync(Guid companyId, Guid requestedByUserId, AddCompanyUserRequest request, CancellationToken cancellationToken = default);
}