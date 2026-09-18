namespace FollowUpApi.DataContext.Entities;

public abstract class CompanyBaseEntity : BaseEntity
{
    public Guid CompanyId { get; set; }

    public Company? Company { get; set; }
}
