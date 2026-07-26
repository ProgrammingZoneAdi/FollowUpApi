namespace FollowUpApi.DataContext.Entities;

public class Company : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string Mobile { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Status { get; set; } = "Active";
}
