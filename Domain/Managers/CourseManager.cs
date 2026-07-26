using FollowUpApi.DataContext;
using FollowUpApi.Domain.Interfaces;

namespace FollowUpApi.Domain.Managers;

public class CourseManager  : ICourseManager
{
    private readonly AppDbContext _dbContext;

    public CourseManager(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
