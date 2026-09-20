using System.Security.Claims;
using FollowUpApi.Common;
using FollowUpApi.Domain.Interfaces;

namespace FollowUpApi.Features.Authentication;

public static class UpdateProfileEndpoint
{
    public static IEndpointRouteBuilder MapUpdateProfileEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/auth/me", HandleAsync).WithName("UpdateMyProfile").WithTags("Authentication").RequireAuthorization()
            .Produces<ApiResponse<LoginUserResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<LoginUserResponse>>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<LoginUserResponse>>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleAsync(UpdateProfileRequest request, ClaimsPrincipal user, IAuthManager manager, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var result = await manager.UpdateProfileAsync(userId, request, cancellationToken);

        if (result.Success)
        {
            return Results.Ok(ApiResponse<LoginUserResponse>.Ok(result.Data!, result.Message));
        }

        var statusCode = result.Status switch
        {
            ServiceResultStatus.ValidationError => StatusCodes.Status400BadRequest,
            ServiceResultStatus.NotFound => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Json(ApiResponse<LoginUserResponse>.Fail(result.Message), statusCode: statusCode);
    }
}
