using System.Text.Json.Serialization;

namespace FollowUpApi.Features.FollowUps;

public sealed class FollowUpResponse
{
    [JsonPropertyName("follow_up_id")]
    public Guid FollowUpId { get; set; }
    [JsonPropertyName("company_id")]
    public Guid CompanyId { get; set; }
    [JsonPropertyName("lead_id")]
    public Guid LeadId { get; set; }
    [JsonPropertyName("follow_up_type")]
    public string FollowUpType { get; set; } = string.Empty;
    [JsonPropertyName("remark")]
    public string Remark { get; set; } = string.Empty;
    [JsonPropertyName("status_after_follow_up")]
    public string StatusAfterFollowUp { get; set; } = string.Empty;
    [JsonPropertyName("follow_up_date")]
    public DateTime FollowUpDate { get; set; }
    [JsonPropertyName("next_follow_up_date")]
    public DateTime? NextFollowUpDate { get; set; }
    [JsonPropertyName("created_by")]
    public Guid? CreatedBy { get; set; }
    [JsonPropertyName("created_on")]
    public DateTime CreatedOn { get; set; }
}
