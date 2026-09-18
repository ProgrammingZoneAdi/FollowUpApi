using System.Text.Json.Serialization;

namespace FollowUpApi.Features.Leads;

public sealed class AssignLeadRequest
{
    // The property must be present. Explicit null means unassign.
    [JsonRequired]
    [JsonPropertyName("assigned_to_user_id")]
    public Guid? AssignedToUserId { get; set; }
}
