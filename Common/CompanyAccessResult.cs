namespace FollowUpApi.Common;

public enum CompanyAccessStatus
{
    Granted,
    CompanyNotFound,
    Forbidden
}
public sealed record CompanyAccessResult(CompanyAccessStatus Status, string? Role = null);
