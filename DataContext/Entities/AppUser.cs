namespace FollowUpApi.DataContext.Entities;

public class AppUser : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Mobile { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Status { get; set; } = "Active";

}
