namespace FollowUpApi.DataContext.Entities;

public class LeadFollowUp : CompanyBaseEntity
{
    public Guid LeadId { get; set; }

    public Lead? Lead { get; set; }

    public DateTime FollowUpDate { get; set; } = DateTime.UtcNow;

    public DateTime? NextFollowUpDate { get; set; }

    public string FollowUpType { get; set; } = "Call";

    public string Remark { get; set; } = string.Empty;

    public string StatusAfterFollowUp { get; set; } = string.Empty;
}
