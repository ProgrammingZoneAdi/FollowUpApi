using FollowUpApi.Common;
using FollowUpApi.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FollowUpApi.Features.CompanyUsers;

public static class CompanyUserEndpoints
{
    public static IEndpointRouteBuilder MapCompanyUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/companies/{companyId:guid}/users", HandleAddCompanyUserAsync).WithName("AddCompanyUser").WithTags("Company Users").RequireAuthorization().Accepts<AddCompanyUserRequest>("application/json").Produces<ApiResponse<AddCompanyUserResponse>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
             .Produces<ApiResponse<AddCompanyUserResponse>>(
                StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<AddCompanyUserResponse>>(
                StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<AddCompanyUserResponse>>(
                StatusCodes.Status404NotFound)
            .Produces<ApiResponse<AddCompanyUserResponse>>(
                StatusCodes.Status409Conflict)
            .Produces<ApiResponse<AddCompanyUserResponse>>(
                StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleAddCompanyUserAsync(Guid companyId,ClaimsPrincipal currentUser, [FromBody] AddCompanyUserRequest request, IUserManager userManager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if(!Guid.TryParse(userIdValue, out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await userManager.AddCompanyUserAsync(companyId, requestedByUserId, request, cancellationToken);

        if (result.Success)
        {
            var response = ApiResponse<AddCompanyUserResponse>.Ok(result.Data, result.Message);

            return Results.Created($"/api/companies/{companyId}/users/" + $"{result.Data!.UserId}", response);
        }

        var error = ApiResponse<AddCompanyUserResponse>.Fail(result.Message);

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
