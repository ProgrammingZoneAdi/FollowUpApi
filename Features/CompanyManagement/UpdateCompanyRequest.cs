using System.Text.Json.Serialization;

namespace FollowUpApi.Features.CompanyManagement;

public sealed class UpdateCompanyRequest
{
    [JsonRequired]
    [JsonPropertyName("company_name")]
    public string CompanyName { get; set; } = string.Empty;

    [JsonRequired]
    [JsonPropertyName("owner_name")]
    public string OwnerName { get; set; } = string.Empty;

    [JsonRequired]
    [JsonPropertyName("mobile")]
    public string Mobile { get; set; } = string.Empty;

    [JsonRequired]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}
