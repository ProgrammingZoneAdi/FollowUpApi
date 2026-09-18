using System.Text.Json.Serialization;

namespace FollowUpApi.Features.CompanyManagement;

public class CompanyOnboardResponse
{
    [JsonPropertyName("company_id")]

    public Guid CompanyId { get; set; }

    [JsonPropertyName("user_id")]

    public Guid UserId { get; set; }

    [JsonPropertyName("link_company_user_id")]

    public Guid LinkCompanyUserId { get; set; }

    [JsonPropertyName("role")]

    public string Role { get; set; } = string.Empty;
}
