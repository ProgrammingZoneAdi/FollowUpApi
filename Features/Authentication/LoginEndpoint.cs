using FollowUpApi.Common;
using FollowUpApi.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FollowUpApi.Features.Authentication;

public static class LoginEndpoint
{
    public static IEndpointRouteBuilder MapLoginEndpoint(
        this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", HandleAsync)
            .WithName("Login")
            .WithTags("Authentication")
            .AllowAnonymous()
            .Accepts<LoginRequest>("application/json")
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] LoginRequest request,
        IAuthManager authManager,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Identification)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(
                ApiResponse<LoginResponse>.Fail(
                    "Identification and password are required"));
        }

        var result = await authManager.LoginAsync(request, cancellationToken);

        if (!result.Success)
        {
            return Results.Json(
                result,
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.Ok(result);
    }
}
