namespace FollowUpApi.Features.Authentication;

public sealed record AccessTokenResult(
    string AccessToken,
    DateTime ExpiresOn);
