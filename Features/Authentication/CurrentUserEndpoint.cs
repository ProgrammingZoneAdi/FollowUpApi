using System.Security.Claims;
using FollowUpApi.Common;
using FollowUpApi.Domain.Interfaces;

namespace FollowUpApi.Features.Authentication;

public static class CurrentUserEndpoint
{
    public static IEndpointRouteBuilder MapCurrentUserEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/me", HandleAsync)
            .WithName("GetCurrentUser")
            .WithTags("Authentication")
            .RequireAuthorization()
            .Produces<ApiResponse<CurrentUserResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<CurrentUserResponse>>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleAsync(ClaimsPrincipal user, IAuthManager authManager, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var result = await authManager.GetCurrentUserAsync(userId, cancellationToken);

        return result.Status switch
        {
            ServiceResultStatus.Success => Results.Ok(ApiResponse<CurrentUserResponse>.Ok(result.Data!, result.Message)),
            ServiceResultStatus.NotFound => Results.Json(ApiResponse<CurrentUserResponse>.Fail(result.Message), statusCode: StatusCodes.Status401Unauthorized),
            _ => Results.Json(ApiResponse<CurrentUserResponse>.Fail(result.Message), statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
