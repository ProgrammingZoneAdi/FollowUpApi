using System.Text.Json.Serialization;

namespace FollowUpApi.Features.Authentication;

public sealed class CurrentUserResponse
{
    [JsonPropertyName("user")]
    public LoginUserResponse User { get; set; } = new();

    [JsonPropertyName("companies")]
    public List<LoginCompanyResponse> Companies { get; set; } = new();
}
