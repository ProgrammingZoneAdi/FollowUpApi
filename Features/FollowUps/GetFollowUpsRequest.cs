using Microsoft.AspNetCore.Mvc;

namespace FollowUpApi.Features.FollowUps;

public sealed class GetFollowUpsRequest
{
    [FromQuery(Name = "page")]

    public int PageNumber { get; set; } = 1;

    [FromQuery(Name = "page_size")]

    public int PageSize { get; set; } = 20;
}
