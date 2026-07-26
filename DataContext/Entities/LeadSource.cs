namespace FollowUpApi.DataContext.Entities;

public class LeadSource : CompanyBaseEntity
{
    public string SourceName { get; set; } = string.Empty;

    public string Status { get; set; } = "Active";
}
