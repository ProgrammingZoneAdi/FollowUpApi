using System.Security.Claims;
using FollowUpApi.Common;
using FollowUpApi.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FollowUpApi.Features.FollowUps;

public static class FollowUpEndpoints
{
    public static IEndpointRouteBuilder MapFollowUpEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/companies/{companyId:guid}/leads/{leadId:guid}/follow-ups", HandleAddFollowUpAsync)
            .WithName("AddFollowUp")
            .WithTags("Follow-ups")
            .RequireAuthorization()
            .Accepts<AddFollowUpRequest>("application/json")
            .Produces<ApiResponse<FollowUpResponse>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<FollowUpResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<FollowUpResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<FollowUpResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<FollowUpResponse>>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse<FollowUpResponse>>(StatusCodes.Status500InternalServerError);
        return app;
    }

    private static async Task<IResult> HandleAddFollowUpAsync(
        Guid companyId, Guid leadId, ClaimsPrincipal currentUser,
        [FromBody] AddFollowUpRequest request, IFollowUpManager manager,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Unauthorized();

        var result = await manager.AddFollowUpAsync(companyId, leadId, userId, request, cancellationToken);
        if (result.Success)
            return Results.Created(
                $"/api/companies/{companyId}/leads/{leadId}/follow-ups/{result.Data!.FollowUpId}",
                ApiResponse<FollowUpResponse>.Ok(result.Data, result.Message));

        var statusCode = result.Status switch
        {
            ServiceResultStatus.ValidationError => StatusCodes.Status400BadRequest,
            ServiceResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ServiceResultStatus.NotFound => StatusCodes.Status404NotFound,
            ServiceResultStatus.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
        return Results.Json(ApiResponse<FollowUpResponse>.Fail(result.Message), statusCode: statusCode);
    }
}
