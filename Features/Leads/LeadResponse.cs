using System.Text.Json.Serialization;

namespace FollowUpApi.Features.Leads;

public sealed class LeadResponse
{
    [JsonPropertyName("lead_id")]

    public Guid LeadId { get; set; }

    [JsonPropertyName("company_id")]

    public Guid CompanyId { get; set; }

    [JsonPropertyName("lead_name")]

    public string LeadName { get; set; } = string.Empty;

    [JsonPropertyName("mobile")]

    public string Mobile { get; set; } = string.Empty;

    [JsonPropertyName("email")]

    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("course_id")]

    public Guid? CourseId { get; set; }

    [JsonPropertyName("course_name")]

    public string? CourseName { get; set; }

    [JsonPropertyName("source_id")]

    public Guid? SourceId { get; set; }

    [JsonPropertyName("source_name")]

    public string? SourceName { get; set; }

    [JsonPropertyName("assigned_to_user_id")]

    public Guid? AssignedToUserId { get; set; }

    [JsonPropertyName("assigned_to_name")]

    public string? AssignedToName { get; set; }

    [JsonPropertyName("status")]

    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("priority")]

    public string Priority { get; set; } = string.Empty;

    [JsonPropertyName("next_follow_up_date")]

    public DateTime? NextFollowUpDate { get; set; }

    [JsonPropertyName("last_follow_up_date")]

    public DateTime? LastFollowUpDate { get; set; }

    [JsonPropertyName("lost_reason")]

    public string LostReason { get; set; } = string.Empty;

    [JsonPropertyName("created_on")]

    public DateTime CreatedOn { get; set; }

    [JsonPropertyName("updated_on")]

    public DateTime? UpdatedOn { get; set; }


}
