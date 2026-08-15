using System.Text.Json.Serialization;

namespace FollowUpApi.Features.LeadSources;

public sealed class CreateLeadSourceRequest
{
    [JsonPropertyName("source_name")]

    public string SourceName { get; set; } = string.Empty;
}

