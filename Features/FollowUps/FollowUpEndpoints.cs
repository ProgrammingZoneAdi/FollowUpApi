using System.Security.Claims;
using FollowUpApi.Common;
using FollowUpApi.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using FollowUpApi.Features.Leads;

namespace FollowUpApi.Features.FollowUps;

public static class FollowUpEndpoints
{
    public static IEndpointRouteBuilder MapFollowUpEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/companies/{companyId:guid}/leads/{leadId:guid}/follow-ups", HandleAddFollowUpAsync).WithName("AddFollowUp").WithTags("Follow-ups").RequireAuthorization()
            .Accepts<AddFollowUpRequest>("application/json")
            .Produces<ApiResponse<FollowUpResponse>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<FollowUpResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<FollowUpResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<FollowUpResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<FollowUpResponse>>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse<FollowUpResponse>>(StatusCodes.Status500InternalServerError);

        app.MapGet("/api/companies/{companyId:guid}/leads/{leadId:guid}/follow-ups", HandleGetFollowUpsAsync).WithName("GetLeadFollowUps").WithTags("Follow-ups").RequireAuthorization()
            .Produces<ApiResponse<PaginationResponse<FollowUpResponse>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<PaginationResponse<FollowUpResponse>>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<PaginationResponse<FollowUpResponse>>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<PaginationResponse<FollowUpResponse>>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<PaginationResponse<FollowUpResponse>>>(StatusCodes.Status500InternalServerError);

        app.MapGet("/api/companies/{companyId:guid}/follow-ups/today", HandleGetTodayFollowUpsAsync).WithName("GetTodayFollowUps").WithTags("Follow-ups").RequireAuthorization()
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status500InternalServerError);

        app.MapGet("/api/companies/{companyId:guid}/follow-ups/overdue", HandleGetOverdueFollowUpsAsync).WithName("GetOverdueFollowUps").WithTags("Follow-ups").RequireAuthorization()
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status500InternalServerError);

        app.MapGet("/api/companies/{companyId:guid}/follow-ups/upcoming", HandleGetUpcomingFollowUpsAsync).WithName("GetUpcomingFollowUps").WithTags("Follow-ups").RequireAuthorization()
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleAddFollowUpAsync(Guid companyId, Guid leadId, ClaimsPrincipal currentUser, [FromBody] AddFollowUpRequest request, IFollowUpManager manager, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await manager.AddFollowUpAsync(companyId, leadId, userId, request, cancellationToken);
        if (result.Success)
        {
            return Results.Created($"/api/companies/{companyId}/leads/{leadId}/follow-ups/{result.Data!.FollowUpId}", ApiResponse<FollowUpResponse>.Ok(result.Data, result.Message));
        }

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

    private static async Task<IResult> HandleGetFollowUpsAsync(Guid companyId, Guid leadId, ClaimsPrincipal currentUser, [AsParameters] GetFollowUpsRequest request, IFollowUpManager manager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await manager.GetFollowUpsAsync(companyId, leadId, userId, request, cancellationToken);

        if (result.Success)
        {
            return Results.Ok(ApiResponse<PaginationResponse<FollowUpResponse>>.Ok(result.Data, result.Message));
        }

        var statusCode = result.Status switch
        {
            ServiceResultStatus.ValidationError => StatusCodes.Status400BadRequest,

            ServiceResultStatus.Forbidden => StatusCodes.Status403Forbidden,

            ServiceResultStatus.NotFound => StatusCodes.Status404NotFound,

            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Json(ApiResponse<PaginationResponse<FollowUpResponse>>.Fail(result.Message), statusCode: statusCode);
    }
    private static async Task<IResult> HandleGetOverdueFollowUpsAsync(Guid companyId, ClaimsPrincipal currentUser, [AsParameters] GetOverdueFollowUpsRequest request, IFollowUpManager manager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await manager.GetOverdueFollowUpsAsync(companyId, userId, request, cancellationToken);

        if (result.Success)
        {
            return Results.Ok(ApiResponse<PaginationResponse<LeadResponse>>.Ok(result.Data, result.Message));
        }

        var statusCode = result.Status switch
        {
            ServiceResultStatus.ValidationError => StatusCodes.Status400BadRequest,
            ServiceResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ServiceResultStatus.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Json(ApiResponse<PaginationResponse<LeadResponse>>.Fail(result.Message), statusCode: statusCode);
    }

    private static async Task<IResult> HandleGetUpcomingFollowUpsAsync(Guid companyId, ClaimsPrincipal currentUser, [AsParameters] GetUpcomingFollowUpsRequest request, IFollowUpManager manager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await manager.GetUpcomingFollowUpsAsync(companyId, userId, request, cancellationToken);

        if (result.Success)
        {
            return Results.Ok(ApiResponse<PaginationResponse<LeadResponse>>.Ok(result.Data, result.Message));
        }

        var statusCode = result.Status switch
        {
            ServiceResultStatus.ValidationError => StatusCodes.Status400BadRequest,
            ServiceResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ServiceResultStatus.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Json(ApiResponse<PaginationResponse<LeadResponse>>.Fail(result.Message), statusCode: statusCode);
    }

    private static async Task<IResult> HandleGetTodayFollowUpsAsync(Guid companyId, ClaimsPrincipal currentUser, [AsParameters] GetTodayFollowUpsRequest request, IFollowUpManager manager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await manager.GetTodayFollowUpsAsync(companyId, userId, request, cancellationToken);

        if (result.Success)
        {
            return Results.Ok(ApiResponse<PaginationResponse<LeadResponse>>.Ok(result.Data, result.Message));
        }

        var statusCode = result.Status switch
        {
            ServiceResultStatus.ValidationError => StatusCodes.Status400BadRequest,
            ServiceResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ServiceResultStatus.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Json(ApiResponse<PaginationResponse<LeadResponse>>.Fail(result.Message), statusCode: statusCode);
    }
}
