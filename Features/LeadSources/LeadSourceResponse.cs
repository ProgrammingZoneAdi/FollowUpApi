using System.Text.Json.Serialization;

namespace FollowUpApi.Features.LeadSources;

public sealed class LeadSourceResponse
{
    [JsonPropertyName("source_id")]

    public Guid SourceId { get; set; }

    [JsonPropertyName("company_id")]

    public Guid CompanyId { get; set; }

    [JsonPropertyName("source_name")]

    public string SourceName { get; set; } = string.Empty;

    [JsonPropertyName("status")]

    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("created_on")]

    public DateTime CreatedOn { get; set; }

    [JsonPropertyName("updated_on")]

    public DateTime? UpdatedOn { get; set; }
}
