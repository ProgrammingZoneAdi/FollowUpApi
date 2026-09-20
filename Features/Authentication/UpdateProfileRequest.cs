using System.Text.Json.Serialization;

namespace FollowUpApi.Features.Authentication;

public sealed class UpdateProfileRequest
{
    [JsonRequired]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
