using System.Text.Json.Serialization;

namespace FollowUpApi.Features.CompanyUsers;

public sealed class AddCompanyUserResponse
{
    [JsonPropertyName("link_company_user_id")]
    public Guid LinkCompanyUserId { get; set; }

    [JsonPropertyName("company_id")]

    public Guid CompanyId { get; set; }

    [JsonPropertyName("user_id")]

    public Guid UserId { get; set; }

    [JsonPropertyName("name")]

    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("mobile")]

    public string Mobile { get; set; } = string.Empty;

    [JsonPropertyName("email")]

    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("role")]

    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("status")]

    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("is_new_user")]

    public bool IsNewUser { get; set; }


}
