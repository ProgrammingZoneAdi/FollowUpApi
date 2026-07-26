namespace FollowUpApi.Common;

public class PaginationRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    public string? SearchText { get; set; }


}
