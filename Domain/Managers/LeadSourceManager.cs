using FollowUpApi.DataContext;
using FollowUpApi.Domain.Interfaces;

namespace FollowUpApi.Domain.Managers;

public class LeadSourceManager : ILeadSourceManager
{
    private readonly AppDbContext _dbContext;

    public LeadSourceManager(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
