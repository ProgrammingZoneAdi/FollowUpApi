using System.Text.Json.Serialization;

namespace FollowUpApi.Features.Leads;

public sealed class CreateLeadRequest
{
    [JsonPropertyName("lead_name")]

    public string LeadName { get; set; } = string.Empty;

    [JsonPropertyName("mobile")]

    public string Mobile { get; set; } = string.Empty;

    [JsonPropertyName("email")]

    public string? Email { get; set; }

    [JsonPropertyName("course_id")]

    public Guid? CourseId { get; set; }

    [JsonPropertyName("source_id")]

    public Guid? SourceId { get; set; }

    [JsonPropertyName("assigned_to_user_id")]

    public Guid? AssignedToUserId { get; set; }

    [JsonPropertyName("priority")]

    public string? Priority { get; set; } = "Normal";

    [JsonPropertyName("next_follow_up_date")]

    public DateTimeOffset? NextFollowUpDate { get; set; }
}
