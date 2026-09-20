using System.Security.Claims;
using FollowUpApi.Common;
using FollowUpApi.Domain.Interfaces;

namespace FollowUpApi.Features.CompanyManagement;

public static class CompanyEndpoints
{
    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/companies/{companyId:guid}", HandleGetCompanyAsync).WithName("GetCompanyDetails").WithTags("Company").RequireAuthorization()
            .Produces<ApiResponse<CompanyDetailsResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<CompanyDetailsResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<CompanyDetailsResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<CompanyDetailsResponse>>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleGetCompanyAsync(Guid companyId, ClaimsPrincipal currentUser, ICompanyManager manager, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var result = await manager.GetCompanyAsync(companyId, userId, cancellationToken);

        if (result.Success)
        {
            return Results.Ok(ApiResponse<CompanyDetailsResponse>.Ok(result.Data!, result.Message));
        }

        var statusCode = result.Status switch
        {
            ServiceResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ServiceResultStatus.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Json(ApiResponse<CompanyDetailsResponse>.Fail(result.Message), statusCode: statusCode);
    }
}
