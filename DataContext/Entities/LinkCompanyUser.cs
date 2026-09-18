namespace FollowUpApi.DataContext.Entities;

public class LinkCompanyUser : BaseEntity
{
    public Guid CompanyId { get; set; }

    public Company? Company { get; set; }

    public Guid UserId { get; set; }

    public AppUser? User { get; set; }

    public string Role { get; set; } = "Staff";
    public bool IsPrimaryOwner { get; set; } = false;

    public string Status { get; set; } = "Active";
}