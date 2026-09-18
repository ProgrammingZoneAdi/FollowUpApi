using System.Text.Json.Serialization;

namespace FollowUpApi.Features.Authentication;

public sealed class LoginResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expires_on")]
    public DateTime ExpiresOn { get; set; }

    [JsonPropertyName("expires_in_seconds")]
    public int ExpiresInSeconds { get; set; }

    [JsonPropertyName("user")]
    public LoginUserResponse User { get; set; } = new();

    [JsonPropertyName("companies")]
    public List<LoginCompanyResponse> Companies { get; set; } = new();
}
