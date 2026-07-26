using FollowUpApi.DataContext;
using FollowUpApi.Domain.Interfaces;

namespace FollowUpApi.Domain.Managers;

public class DashboardManager : IDashboardManager
{
    private readonly AppDbContext _dbContext;

    public DashboardManager(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
