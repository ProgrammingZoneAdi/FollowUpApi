using FollowUpApi.Common;
using FollowUpApi.Features.CompanyManagement;
using FollowUpApi.Features.Courses;

namespace FollowUpApi.Domain.Interfaces;

public interface ICompanyManager
{
    Task<ServiceResult<CompanyDetailsResponse>> UpdateCompanyAsync(Guid companyId, Guid requestedByUserId, UpdateCompanyRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<CompanyDetailsResponse>> GetCompanyAsync(Guid companyId, Guid requestedByUserId, CancellationToken cancellationToken = default);

    public Task<ApiResponse<CompanyOnboardResponse>> OnboardCompanyAsync(CompanyOnboardRequest request, CancellationToken cancellationToken = default);
}
