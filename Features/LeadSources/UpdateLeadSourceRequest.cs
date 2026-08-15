using System.Text.Json.Serialization;

namespace FollowUpApi.Features.LeadSources;

public sealed class UpdateLeadSourceRequest
{
    [JsonPropertyName("source_name")]
    public string SourceName { get; set; } = string.Empty;
}
