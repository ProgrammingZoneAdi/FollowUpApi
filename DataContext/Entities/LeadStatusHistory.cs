namespace FollowUpApi.DataContext.Entities;

public class LeadStatusHistory : CompanyBaseEntity
{
    public Guid LeadId { get; set; }

    public Lead? Lead { get; set; }

    public string OldStatus { get; set; } = string.Empty;

    public string NewStatus { get; set; } = string.Empty;

    public string Remarks { get; set; } = string.Empty;
}
