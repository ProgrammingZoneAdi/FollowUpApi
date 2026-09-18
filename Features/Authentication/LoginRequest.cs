using System.Text.Json.Serialization;

namespace FollowUpApi.Features.Authentication;

public sealed class LoginRequest
{
    [JsonPropertyName("identification")]
    public string Identification { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}
