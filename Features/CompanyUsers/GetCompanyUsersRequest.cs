using Microsoft.AspNetCore.Mvc;

namespace FollowUpApi.Features.CompanyUsers;

public sealed class GetCompanyUsersRequest
{

    [FromQuery(Name = "Page")]

    public int PageNumber { get; set; } = 1;

    [FromQuery(Name = "page_size")]
    public int PageSize { get; set; } = 20;

    [FromQuery(Name = "search")]

    public string? Search { get; set; }

    [FromQuery(Name = "role")]

    public string? Role { get; set; }

    [FromQuery(Name = "status")]

    public string? Status { get; set; } = "Active";

}
