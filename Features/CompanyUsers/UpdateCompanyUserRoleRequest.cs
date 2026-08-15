using System.Text.Json.Serialization;

namespace FollowUpApi.Features.CompanyUsers;

public sealed class UpdateCompanyUserRoleRequest
{
    [JsonPropertyName("role")]

    public string Role { get; set; } = string.Empty;
}
