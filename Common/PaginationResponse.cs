using System.Text.Json.Serialization;

namespace FollowUpApi.Common;

public sealed class PaginationResponse<T>
{
    [JsonPropertyName("rows")]
    public List<T> Rows { get; set; } = new();

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set;}

    [JsonPropertyName("page_number")]
    public int PageNumber { get; set;}

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    [JsonPropertyName("total_pages")]

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
