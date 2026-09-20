using System.Security.Claims;
using FollowUpApi.Common;
using FollowUpApi.Domain.Interfaces;

namespace FollowUpApi.Features.CompanyManagement;

public static class UpdateCompanyEndpoint
{
    public static IEndpointRouteBuilder MapUpdateCompanyEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/companies/{companyId:guid}", HandleAsync).WithName("UpdateCompanyDetails").WithTags("Company").RequireAuthorization()
            .Produces<ApiResponse<CompanyDetailsResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<CompanyDetailsResponse>>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<CompanyDetailsResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<CompanyDetailsResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<CompanyDetailsResponse>>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse<CompanyDetailsResponse>>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleAsync(Guid companyId, UpdateCompanyRequest request, ClaimsPrincipal user, ICompanyManager manager, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var result = await manager.UpdateCompanyAsync(companyId, userId, request, cancellationToken);

        if (result.Success)
        {
            return Results.Ok(ApiResponse<CompanyDetailsResponse>.Ok(result.Data!, result.Message));
        }

        var statusCode = result.Status switch
        {
            ServiceResultStatus.ValidationError => StatusCodes.Status400BadRequest,
            ServiceResultStatus.NotFound => StatusCodes.Status404NotFound,
            ServiceResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ServiceResultStatus.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Json(ApiResponse<CompanyDetailsResponse>.Fail(result.Message), statusCode: statusCode);
    }
}
