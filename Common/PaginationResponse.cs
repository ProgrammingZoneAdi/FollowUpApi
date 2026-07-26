namespace FollowUpApi.Common;

public class PaginationResponse<T>
{
    public List<T> Rows { get; set; } = new();

    public int TotalCount { get; set;}

    public int PageNumber { get; set;}

    public int PageSize { get; set; }
}
