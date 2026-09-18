using Microsoft.AspNetCore.Mvc;

namespace FollowUpApi.Features.LeadSources;

public sealed class GetLeadSourcesRequest
{
    [FromQuery(Name = "page")]

    public int PageNumber { get; set; } = 1;

    [FromQuery(Name = "page_size")]

    public int PageSize { get; set; } = 20;

    [FromQuery(Name = "search")]

    public string? Search { get; set; }

    [FromQuery(Name = "status")]

    public string? Status { get; set; } = "Active";
}
