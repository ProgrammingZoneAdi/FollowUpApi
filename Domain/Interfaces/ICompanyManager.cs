using FollowUpApi.Common;
using FollowUpApi.Features.CompanyManagement;

namespace FollowUpApi.Domain.Interfaces;

public interface ICompanyManager
{
    public Task<ApiResponse<CompanyOnboardResponse>> OnboardCompanyAsync(CompanyOnboardRequest request, CancellationToken cancellationToken = default);
}
