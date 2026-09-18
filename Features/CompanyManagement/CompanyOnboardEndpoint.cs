using FollowUpApi.Common;
using FollowUpApi.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FollowUpApi.Features.CompanyManagement;

public static class CompanyOnboardEndpoint
{
    public static IEndpointRouteBuilder MapCompanyOnboardEndpont(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/company/onboard", HandleAsync).WithName("CompanyOnboard").WithTags("Company Management").Accepts<CompanyOnboardRequest>("application/json")
            .Produces<ApiResponse<CompanyOnboardResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<CompanyOnboardResponse>>(StatusCodes.Status400BadRequest);

        return app;
    }

    public static async Task<IResult> HandleAsync([FromBody] CompanyOnboardRequest request, ICompanyManager companyManager, CancellationToken cancellationToken)
    {
        var result = await companyManager.OnboardCompanyAsync(request, cancellationToken);

        if (!result.Success)
            return Results.BadRequest(result);

        return Results.Created($"/api/company/{result.Data!.CompanyId}", result);
    }    
}
