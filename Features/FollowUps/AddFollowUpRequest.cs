using System.Text.Json.Serialization;

namespace FollowUpApi.Features.FollowUps;

public sealed class AddFollowUpRequest
{
    [JsonPropertyName("follow_up_type")]
    public string FollowUpType { get; set; } = "Call";

    [JsonRequired, JsonPropertyName("remark")]
    public string Remark { get; set; } = string.Empty;

    [JsonRequired, JsonPropertyName("status_after_follow_up")]
    public string StatusAfterFollowUp { get; set; } = string.Empty;

    // Explicit null clears the next appointment; omission is rejected.
    [JsonRequired, JsonPropertyName("next_follow_up_date")]
    public DateTimeOffset? NextFollowUpDate { get; set; }

    [JsonPropertyName("lost_reason")]
    public string? LostReason { get; set; }
}
