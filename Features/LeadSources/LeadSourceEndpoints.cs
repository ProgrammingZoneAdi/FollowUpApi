using FollowUpApi.Common;
using FollowUpApi.Domain.Interfaces;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FollowUpApi.Features.LeadSources;

public static class LeadSourceEndpoints
{
    public static IEndpointRouteBuilder MapLeadSourceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/companies/{companyId:guid}/lead-sources", HandleCreateLeadSourceAsync).WithName("CreateLeadSource").WithTags("Lead Sources").RequireAuthorization()
            .Accepts<CreateLeadSourceRequest>("application/json")
            .Produces<ApiResponse<LeadSourceResponse>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<LeadSourceResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<LeadSourceResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<LeadSourceResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<LeadSourceResponse>>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse<LeadSourceResponse>>(StatusCodes.Status500InternalServerError);

        app.MapGet("/api/companies/{companyId:guid}/lead-sources", HandleGetLeadSourcesAsync).WithName("GetLeadSources").WithTags("Lead Sources").RequireAuthorization()
            .Produces<ApiResponse<PaginationResponse<LeadSourceResponse>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<PaginationResponse<LeadSourceResponse>>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<PaginationResponse<LeadSourceResponse>>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<PaginationResponse<LeadSourceResponse>>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<PaginationResponse<LeadSourceResponse>>>(StatusCodes.Status500InternalServerError);

        app.MapPut("/api/companies/{companyId:guid}/lead-sources/{sourceId:guid}", HandleUpdateLeadSourceAsync).WithName("UpdateLeadSource").WithTags("Lead Sources").RequireAuthorization()
            .Accepts<UpdateLeadSourceRequest>("application/json")
            .Produces<ApiResponse<LeadSourceResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<LeadSourceResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<LeadSourceResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<LeadSourceResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<LeadSourceResponse>>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse<LeadSourceResponse>>(StatusCodes.Status500InternalServerError);

        app.MapDelete("/api/companies/{companyId:guid}/lead-sources/{sourceId:guid}", HandleDeactivateLeadSourceAsync).WithName("DeactivateLeadSource").WithTags("Lead Sources").RequireAuthorization()
            .Produces<ApiResponse<LeadSourceResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<LeadSourceResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<LeadSourceResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<LeadSourceResponse>>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse<LeadSourceResponse>>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleCreateLeadSourceAsync(Guid companyId, ClaimsPrincipal currentUser, [FromBody] CreateLeadSourceRequest request, ILeadSourceManager leadSourceManager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if(!Guid.TryParse(userIdValue, out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await leadSourceManager.CreateLeadSourceAsync(companyId, requestedByUserId, request, cancellationToken);

        if (result.Success)
        {
            var response = ApiResponse<LeadSourceResponse>.Ok(result.Data, result.Message);

            return Results.Created($"/api/companies/{companyId}/lead-sources/{result.Data!.SourceId}", response);

        }

        return MapError(result);
    }

    private static async Task<IResult> HandleGetLeadSourcesAsync(Guid companyId, ClaimsPrincipal currentUser, [AsParameters] GetLeadSourcesRequest request, ILeadSourceManager leadSourceManager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if(!Guid.TryParse(userIdValue, out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await leadSourceManager.GetLeadSourcesAsync(companyId, requestedByUserId, request, cancellationToken);

        if (result.Success)
        {
            var response = ApiResponse<PaginationResponse<LeadSourceResponse>>.Ok(result.Data, result.Message);

            return Results.Ok(response);
        }

        return MapError(result);
    }

    private static async Task<IResult> HandleUpdateLeadSourceAsync(Guid companyId, Guid sourceId, ClaimsPrincipal currentUser, [FromBody] UpdateLeadSourceRequest request, ILeadSourceManager leadSourceManager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if(!Guid.TryParse(userIdValue, out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await leadSourceManager.UpdateLeadSourceAsync(companyId, sourceId, requestedByUserId, request, cancellationToken);

        if (result.Success)
        {
            var response = ApiResponse<LeadSourceResponse>.Ok(result.Data, result.Message);
            return Results.Ok(response);
        }

        return MapError(result);
    }

    private static async Task<IResult> HandleDeactivateLeadSourceAsync(Guid companyId, Guid sourceId, ClaimsPrincipal currentUser, ILeadSourceManager leadSourceManager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if(!Guid.TryParse(userIdValue, out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await leadSourceManager.DeactivateLeadSourceAsync(companyId, sourceId, requestedByUserId, cancellationToken);

        if (result.Success)
        {
            var response = ApiResponse<LeadSourceResponse>.Ok(result.Data, result.Message);
            return Results.Ok(response);
        }

        return MapError(result);
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
