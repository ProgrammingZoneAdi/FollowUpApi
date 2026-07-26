namespace FollowUpApi.DataContext.Entities;

public class Lead : CompanyBaseEntity
{
    public string LeadName { get; set; } = string.Empty;

    public string Mobile { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public Guid? CourseId { get; set; }

    public Course? Course { get; set; }

    public Guid? SourceId { get; set; }

    public LeadSource? Source { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public AppUser? AssignedToUser { get; set; }


    public string Status { get; set; } = "New";

    public string Priority { get; set; } = "Normal";

    public DateTime? NextFollowUpDate { get; set; }

    public DateTime? LastFollowUpDate { get; set; }

    public string LostReason { get; set; } = string.Empty;
}
