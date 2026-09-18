using System.Text.Json.Serialization;

namespace FollowUpApi.Features.Authentication;

public sealed class LoginCompanyResponse
{
    [JsonPropertyName("company_id")]
    public Guid CompanyId { get; set; }

    [JsonPropertyName("company_name")]
    public string CompanyName { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("is_primary_owner")]
    public bool IsPrimaryOwner { get; set; }
}
