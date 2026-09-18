using System.Text.Json.Serialization;

namespace FollowUpApi.Features.Leads;

public sealed class DeactivateLeadResponse
{
    [JsonPropertyName("lead_id")]
    public Guid LeadId { get; set; }

    [JsonPropertyName("company_id")]
    public Guid CompanyId { get; set; }

    [JsonPropertyName("is_deleted")]
    public bool IsDeleted { get; set; }

    [JsonPropertyName("updated_on")]
    public DateTime? UpdatedOn { get; set; }
}
