using FollowUpApi.DataContext;
using FollowUpApi.Domain.Interfaces;

namespace FollowUpApi.Domain.Managers;

public class LeadManager : ILeadManager
{
    private readonly AppDbContext _dbContext;

    public LeadManager(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
