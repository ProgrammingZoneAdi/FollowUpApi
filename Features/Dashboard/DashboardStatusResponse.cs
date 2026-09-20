using System.Text.Json.Serialization;

namespace FollowUpApi.Features.Dashboard;

public sealed class DashboardStatusResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }
}
