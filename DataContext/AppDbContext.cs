using FollowUpApi.DataContext.Entities;
using Microsoft.EntityFrameworkCore;

namespace FollowUpApi.DataContext;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies { get; set; }

    public DbSet<AppUser> AppUsers { get; set; }

    public DbSet<LinkCompanyUser> LinkCompanyUsers { get; set; }

    public DbSet<Course> Courses { get; set; }

    public DbSet<LeadSource> LeadSources { get; set; }

    public DbSet<Lead> Leads { get; set; }

    public DbSet<LeadFollowUp> LeadFollowUps { get; set; }

    public DbSet<LeadStatusHistory> LeadStatusHistories { get; set; }
}
