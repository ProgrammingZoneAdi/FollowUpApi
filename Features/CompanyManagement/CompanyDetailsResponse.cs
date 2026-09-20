using System.Text.Json.Serialization;

namespace FollowUpApi.Features.CompanyManagement;

public sealed class CompanyDetailsResponse
{
    [JsonPropertyName("company_id")]
    public Guid CompanyId { get; set; }

    [JsonPropertyName("company_name")]
    public string CompanyName { get; set; } = string.Empty;

    [JsonPropertyName("owner_name")]
    public string OwnerName { get; set; } = string.Empty;

    [JsonPropertyName("mobile")]
    public string Mobile { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("created_on")]
    public DateTime CreatedOn { get; set; }

    [JsonPropertyName("updated_on")]
    public DateTime? UpdatedOn { get; set; }
}
