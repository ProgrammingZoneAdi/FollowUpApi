using System.Text.Json.Serialization;

namespace FollowUpApi.Features.Courses;

public sealed class CourseResponse
{
    [JsonPropertyName("course_id")]
    public Guid CourseId { get; set; }

    [JsonPropertyName("company_id")]

    public Guid CompanyId { get; set; }

    [JsonPropertyName("course_name")]

    public string CourseName { get; set; } = string.Empty;

    [JsonPropertyName("fees")]

    public decimal? Fees { get; set; }

    [JsonPropertyName("duration")]

    public string Duration { get; set; } = string.Empty;

    [JsonPropertyName("status")]

    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("created_on")]

    public DateTime CreatedOn { get; set; }

    [JsonPropertyName("updated_on")]

    public DateTime? UpdatedOn { get; set; }
}
