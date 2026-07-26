using FollowUpApi.DataContext;
using FollowUpApi.Domain.Interfaces;

namespace FollowUpApi.Domain.Managers;

public class FollowUpManager : IFollowUpManager
{
    private readonly AppDbContext _dbContext;

    public FollowUpManager(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
}

