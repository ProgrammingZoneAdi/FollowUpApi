using Microsoft.AspNetCore.Mvc;

namespace FollowUpApi.Features.Leads;

public sealed class GetLeadsRequest
{
    [FromQuery(Name = "page")]

    public int PageNumber { get; set; } = 1;

    [FromQuery(Name = "page_size")]

    public int PageSize { get; set; } = 20;

    [FromQuery(Name = "search")]

    public string? Search { get; set; }

    [FromQuery(Name = "status")]

    public string? Status { get; set; } = "All";

    [FromQuery(Name = "priority")]
    public string? Priority { get; set; } = "All";

    [FromQuery(Name = "course_id")]

    public Guid? CourseId { get; set; }

    [FromQuery(Name = "source_id")]

    public Guid? SourceId { get; set; }

    [FromQuery(Name = "assigned_to_user_id")]

    public Guid? AssignedToUserId { get; set; }
}
