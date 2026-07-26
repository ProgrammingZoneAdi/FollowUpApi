using FollowUpApi.Common;
using FollowUpApi.DataContext;
using FollowUpApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FollowUpApi.Domain.Managers;

public sealed class CompanyAccessService : ICompanyAccessService
{
    private const string ActiveStatus = "Active";

    private readonly AppDbContext _dbContext;

    public CompanyAccessService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CompanyAccessResult> CheckAccessAsync(Guid companyId, Guid userId, IReadOnlyCollection<string> allowedRoles, CancellationToken cancellationToken = default)
    {
        var membership = await _dbContext.LinkCompanyUsers.AsNoTracking().Where(link => !link.IsDeleted && link.CompanyId == companyId && link.UserId == userId && link.Status == ActiveStatus && link.Company != null && !link.Company.IsDeleted && link.Company.Status == ActiveStatus)
            .Select(link => new
            {
                link.Role
            }).FirstOrDefaultAsync(cancellationToken);


        if(membership is null)
        {
            var companyExists = await _dbContext.Companies.AsNoTracking().AnyAsync(company => company.Id == companyId && !company.IsDeleted && company.Status == ActiveStatus, cancellationToken);

            if (!companyExists)
            {
                return new CompanyAccessResult(CompanyAccessStatus.CompanyNotFound);
            }

            return new CompanyAccessResult(CompanyAccessStatus.Forbidden);
        }

        var roleIsAllowed = allowedRoles.Count == 0 || allowedRoles.Any(role => string.Equals(role, membership.Role, StringComparison.OrdinalIgnoreCase));

        if (!roleIsAllowed)
        {
            return new CompanyAccessResult(CompanyAccessStatus.Forbidden, membership.Role);
        }

        return new CompanyAccessResult(CompanyAccessStatus.Granted, membership.Role);

    }

 

}
