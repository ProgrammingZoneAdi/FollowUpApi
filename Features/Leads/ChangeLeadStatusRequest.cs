using System.Text.Json.Serialization;

namespace FollowUpApi.Features.Leads;

public sealed class ChangeLeadStatusRequest
{
    [JsonRequired]
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("lost_reason")]
    public string? LostReason { get; set; }

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }
}
