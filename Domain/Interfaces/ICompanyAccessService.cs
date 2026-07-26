using FollowUpApi.Common;

namespace FollowUpApi.Domain.Interfaces;

public interface ICompanyAccessService
{
    public Task<CompanyAccessResult> CheckAccessAsync(Guid companyId, Guid userId, IReadOnlyCollection<string> allowedRoles, CancellationToken cancellationToken = default);
}
