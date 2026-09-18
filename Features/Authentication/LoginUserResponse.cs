using System.Text.Json.Serialization;

namespace FollowUpApi.Features.Authentication;

public sealed class LoginUserResponse
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("mobile")]
    public string Mobile { get; set; } = string.Empty;
}
