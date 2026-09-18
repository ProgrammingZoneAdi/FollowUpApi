using FollowUpApi.Common;
using FollowUpApi.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace FollowUpApi.Features.Leads;

public static class LeadEndpoints
{
    public static IEndpointRouteBuilder MapLeadEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/companies/{companyId:guid}/leads", HandleCreateLeadAsync).WithName("CreateLead").WithTags("Leads").RequireAuthorization()
           .Accepts<CreateLeadRequest>("application/json")
           .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status201Created)
           .Produces(StatusCodes.Status401Unauthorized)
           .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status400BadRequest)
           .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status403Forbidden)
           .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status404NotFound)
           .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status409Conflict)
           .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status500InternalServerError);

        app.MapGet("/api/companies/{companyId:guid}/leads", HandleGetLeadsAsync).WithName("GetLeads").WithTags("Leads").RequireAuthorization()
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<PaginationResponse<LeadResponse>>>(StatusCodes.Status500InternalServerError);

        app.MapGet("/api/companies/{companyId:guid}/leads/{leadId:guid}", HandleGetLeadDetailsAsync).WithName("GetLeadDetails").WithTags("Leads").RequireAuthorization()
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status500InternalServerError);

        app.MapPut("/api/companies/{companyId:guid}/leads/{leadId:guid}", HandleUpdateLeadAsync)
            .WithName("UpdateLead")
            .WithTags("Leads")
            .RequireAuthorization()
            .Accepts<UpdateLeadRequest>("application/json")
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status500InternalServerError);

        app.MapPatch("/api/companies/{companyId:guid}/leads/{leadId:guid}/assignment", HandleAssignLeadAsync)
            .WithName("AssignLead")
            .WithTags("Leads")
            .RequireAuthorization()
            .Accepts<AssignLeadRequest>("application/json")
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status500InternalServerError);

        app.MapPatch("/api/companies/{companyId:guid}/leads/{leadId:guid}/status", HandleChangeLeadStatusAsync)
            .WithName("ChangeLeadStatus")
            .WithTags("Leads")
            .RequireAuthorization()
            .Accepts<ChangeLeadStatusRequest>("application/json")
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse<LeadResponse>>(StatusCodes.Status500InternalServerError);

        app.MapDelete("/api/companies/{companyId:guid}/leads/{leadId:guid}", HandleDeactivateLeadAsync)
            .WithName("DeactivateLead")
            .WithTags("Leads")
            .RequireAuthorization()
            .Produces<ApiResponse<DeactivateLeadResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<DeactivateLeadResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<DeactivateLeadResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<DeactivateLeadResponse>>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse<DeactivateLeadResponse>>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleCreateLeadAsync(Guid companyId, ClaimsPrincipal currentUser, [FromBody] CreateLeadRequest request, ILeadManager leadManager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if(!Guid.TryParse(userIdValue, out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await leadManager.CreateLeadAsync(companyId, requestedByUserId, request, cancellationToken);

        if (result.Success)
        {
            var response = ApiResponse<LeadResponse>.Ok(result.Data, result.Message);

            return Results.Created($"/api/companies/{companyId}/leads/{result.Data!.LeadId}", response);
        }

        return MapError(result);
    }

    private static async Task<IResult> HandleGetLeadsAsync(Guid companyId, ClaimsPrincipal currentUser, [AsParameters] GetLeadsRequest request, ILeadManager leadManager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await leadManager.GetLeadsAsync(companyId, requestedByUserId, request, cancellationToken);

        if (result.Success)
        {
            var response = ApiResponse<PaginationResponse<LeadResponse>>.Ok(result.Data, result.Message);
            return Results.Ok(response);
        }

        return MapError(result);
    }

    private static async Task<IResult> HandleGetLeadDetailsAsync(Guid companyId, Guid leadId, ClaimsPrincipal currentUser, ILeadManager leadManager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if(!Guid.TryParse(userIdValue, out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await leadManager.GetLeadDetailsAsync(companyId, leadId, requestedByUserId, cancellationToken);

        if (result.Success)
        {
            return Results.Ok(ApiResponse<LeadResponse>.Ok(result.Data, result.Message));
        }

        return MapError(result);
    }

    private static async Task<IResult> HandleUpdateLeadAsync(
        Guid companyId,
        Guid leadId,
        ClaimsPrincipal currentUser,
        [FromBody] UpdateLeadRequest request,
        ILeadManager leadManager,
        CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await leadManager.UpdateLeadAsync(
            companyId, leadId, requestedByUserId, request, cancellationToken);

        if (result.Success)
        {
            return Results.Ok(ApiResponse<LeadResponse>.Ok(result.Data, result.Message));
        }

        return MapError(result);
    }

    private static async Task<IResult> HandleAssignLeadAsync(
        Guid companyId,
        Guid leadId,
        ClaimsPrincipal currentUser,
        [FromBody] AssignLeadRequest request,
        ILeadManager leadManager,
        CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await leadManager.AssignLeadAsync(
            companyId, leadId, requestedByUserId, request, cancellationToken);

        if (result.Success)
        {
            return Results.Ok(ApiResponse<LeadResponse>.Ok(result.Data, result.Message));
        }

        return MapError(result);
    }

    private static async Task<IResult> HandleChangeLeadStatusAsync(
        Guid companyId,
        Guid leadId,
        ClaimsPrincipal currentUser,
        [FromBody] ChangeLeadStatusRequest request,
        ILeadManager leadManager,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.FindFirstValue(ClaimTypes.NameIdentifier),
                out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await leadManager.ChangeLeadStatusAsync(
            companyId, leadId, requestedByUserId, request, cancellationToken);

        return result.Success
            ? Results.Ok(ApiResponse<LeadResponse>.Ok(result.Data, result.Message))
            : MapError(result);
    }

    private static async Task<IResult> HandleDeactivateLeadAsync(
        Guid companyId,
        Guid leadId,
        ClaimsPrincipal currentUser,
        ILeadManager leadManager,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.FindFirstValue(ClaimTypes.NameIdentifier),
                out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await leadManager.DeactivateLeadAsync(
            companyId, leadId, requestedByUserId, cancellationToken);

        return result.Success
            ? Results.Ok(ApiResponse<DeactivateLeadResponse>.Ok(result.Data, result.Message))
            : MapError(result);
    }

    private static IResult MapError<T>(ServiceResult<T> result)
    {
        var error = ApiResponse<T>.Fail(result.Message);

        var statusCode = result.Status switch
        {
            ServiceResultStatus.ValidationError => StatusCodes.Status400BadRequest,
            ServiceResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ServiceResultStatus.NotFound => StatusCodes.Status404NotFound,
            ServiceResultStatus.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Json(error, statusCode: statusCode);
    }
}
