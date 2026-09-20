using System.Text.Json.Serialization;

namespace FollowUpApi.Features.Authentication;

public sealed class ChangePasswordRequest
{
    [JsonRequired]
    [JsonPropertyName("current_password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [JsonRequired]
    [JsonPropertyName("new_password")]
    public string NewPassword { get; set; } = string.Empty;
}
