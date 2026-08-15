using System.Text.Json.Serialization;

namespace FollowUpApi.Features.Courses;

public sealed class UpdateCourseRequest
{
    [JsonPropertyName("course_name")]

    public string CourseName { get; set; } = string.Empty;

    [JsonPropertyName("fees")]

    public decimal? Fees { get; set; }

    [JsonPropertyName("duration")]

    public string Duration { get; set; } = string.Empty;
}
