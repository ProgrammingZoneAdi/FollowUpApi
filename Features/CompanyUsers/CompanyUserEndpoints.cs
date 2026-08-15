using System.Security.Claims;
using FollowUpApi.Common;
using FollowUpApi.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FollowUpApi.Features.CompanyUsers;

public static class CompanyUserEndpoints
{
    public static IEndpointRouteBuilder MapCompanyUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/companies/{companyId:guid}/users", HandleAddCompanyUserAsync).WithName("AddCompanyUser").WithTags("Company Users").RequireAuthorization()
            .Accepts<AddCompanyUserRequest>("application/json")
            .Produces<ApiResponse<AddCompanyUserResponse>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<AddCompanyUserResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<AddCompanyUserResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<AddCompanyUserResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<AddCompanyUserResponse>>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse<AddCompanyUserResponse>>(StatusCodes.Status500InternalServerError);

        app.MapGet("/api/companies/{companyId:guid}/users", HandleGetCompanyUsersAsync).WithName("GetCompanyUsers").WithTags("Company Users").RequireAuthorization()
            .Produces<ApiResponse<PaginationResponse<CompanyUserListItemResponse>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<PaginationResponse<CompanyUserListItemResponse>>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<PaginationResponse<CompanyUserListItemResponse>>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<PaginationResponse<CompanyUserListItemResponse>>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<PaginationResponse<CompanyUserListItemResponse>>>(StatusCodes.Status500InternalServerError);

        app.MapPatch("/api/companies/{companyId:guid}/users/" + "{userId:guid}/role", HandleUpdateCompanyUserRoleAsync).WithName("UpdateCompanyUserRole").WithTags("Company Users").RequireAuthorization()
            .Accepts<UpdateCompanyUserRoleRequest>("application/json")
            .Produces<ApiResponse<CompanyUserListItemResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<CompanyUserListItemResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<CompanyUserListItemResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<CompanyUserListItemResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<CompanyUserListItemResponse>>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse<CompanyUserListItemResponse>>(StatusCodes.Status500InternalServerError);

        app.MapDelete("/api/companies/{companyId:guid}/users/" + "{userId:guid}", HandleDeactivateCompanyUserAsync).WithName("DeactivateCompanyUser").WithTags("Company Users").RequireAuthorization()
            .Produces<ApiResponse<CompanyUserListItemResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<CompanyUserListItemResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<CompanyUserListItemResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<CompanyUserListItemResponse>>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse<CompanyUserListItemResponse>>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleAddCompanyUserAsync(Guid companyId, ClaimsPrincipal currentUser, [FromBody] AddCompanyUserRequest request, IUserManager userManager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await userManager.AddCompanyUserAsync(companyId,requestedByUserId, request, cancellationToken);

        if (result.Success)
        {
            var response = ApiResponse<AddCompanyUserResponse>.Ok(result.Data, result.Message);

            return Results.Created($"/api/companies/{companyId}/users/" + $"{result.Data!.UserId}", response);
        }

        return MapError<AddCompanyUserResponse>(result);
    }

    private static async Task<IResult> HandleGetCompanyUsersAsync(Guid companyId, ClaimsPrincipal currentUser, [AsParameters] GetCompanyUsersRequest request, IUserManager userManager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await userManager.GetCompanyUsersAsync(companyId, requestedByUserId, request, cancellationToken);

        if (result.Success)
        {
            var response = ApiResponse<PaginationResponse<CompanyUserListItemResponse>>.Ok(result.Data,result.Message);

            return Results.Ok(response);
        }

        return MapError<PaginationResponse<CompanyUserListItemResponse>>(result);
    }

    private static async Task<IResult> HandleUpdateCompanyUserRoleAsync(Guid companyId, Guid userId, ClaimsPrincipal currentUser, [FromBody] UpdateCompanyUserRoleRequest request, IUserManager userManager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if(!Guid.TryParse(userIdValue, out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await userManager.UpdateCompanyUserRoleAsync(companyId, userId, requestedByUserId, request, cancellationToken);

        if (result.Success)
        {
            var response = ApiResponse<CompanyUserListItemResponse>.Ok(result.Data, result.Message);

            return Results.Ok(response);
        }

        return MapError<CompanyUserListItemResponse>(result);
    }

    private static async Task<IResult> HandleDeactivateCompanyUserAsync(Guid companyId, Guid userId, ClaimsPrincipal currentUser, IUserManager userManager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if(!Guid.TryParse(userIdValue, out var requestedByUserId)) { return Results.Unauthorized(); }

        var result = await userManager.DeactivateCompanyUserAsync(companyId, userId, requestedByUserId, cancellationToken);

        if (result.Success)
        {
            var response = ApiResponse<CompanyUserListItemResponse>.Ok(result.Data, result.Message);

            return Results.Ok(response);
        }
        return MapError<CompanyUserListItemResponse>(result);
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

        return Results.Json(error,statusCode: statusCode);
    }
}