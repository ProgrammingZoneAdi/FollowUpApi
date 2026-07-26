using System.Text.Json.Serialization;

namespace FollowUpApi.Features.CompanyUsers;

public sealed class AddCompanyUserRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("mobile")]

    public string Mobile { get; set; } = string.Empty;

    [JsonPropertyName("email")]

    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]

    public string? Password { get; set; }

    [JsonPropertyName("role")]

    public string Role { get; set; } = "Staff";
}
