using System.Text.Json.Serialization;

namespace FollowUpApi.Features.CompanyManagement;

public class CompanyOnboardRequest
{
    [JsonPropertyName("company_name")]

    public string CompanyName { get; set; } = string.Empty;

    [JsonPropertyName("owner_name")]

    public string OwnerName { get; set; } = string.Empty;

    [JsonPropertyName("mobile")]

    public string Mobile { get; set; } = string.Empty;

    [JsonPropertyName("email")]

    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]

    public string Password { get; set; } = string.Empty;
 }
