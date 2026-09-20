using System.Text.Json.Serialization;

namespace FollowUpApi.Features.Authentication;

public sealed class ChangePasswordResponse
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("updated_on")]
    public DateTime UpdatedOn { get; set; }
}
