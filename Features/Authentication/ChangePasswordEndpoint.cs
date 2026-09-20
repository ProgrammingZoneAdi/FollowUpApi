using System.Security.Claims;
using FollowUpApi.Common;
using FollowUpApi.Domain.Interfaces;

namespace FollowUpApi.Features.Authentication;

public static class ChangePasswordEndpoint
{
    public static IEndpointRouteBuilder MapChangePasswordEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/change-password", HandleAsync)
            .WithName("ChangePassword")
            .WithTags("Authentication")
            .RequireAuthorization()
            .Accepts<ChangePasswordRequest>("application/json")
            .Produces<ApiResponse<ChangePasswordResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<ChangePasswordResponse>>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<ChangePasswordResponse>>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse<ChangePasswordResponse>>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleAsync(ClaimsPrincipal user, ChangePasswordRequest request, IAuthManager authManager, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var result = await authManager.ChangePasswordAsync(userId, request, cancellationToken);

        return result.Status switch
        {
            ServiceResultStatus.Success => Results.Ok(ApiResponse<ChangePasswordResponse>.Ok(result.Data!, result.Message)),
            ServiceResultStatus.ValidationError => Results.BadRequest(ApiResponse<ChangePasswordResponse>.Fail(result.Message)),
            ServiceResultStatus.NotFound => Results.Json(ApiResponse<ChangePasswordResponse>.Fail(result.Message), statusCode: StatusCodes.Status401Unauthorized),
            ServiceResultStatus.Conflict => Results.Conflict(ApiResponse<ChangePasswordResponse>.Fail(result.Message)),
            _ => Results.Json(ApiResponse<ChangePasswordResponse>.Fail(result.Message), statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
