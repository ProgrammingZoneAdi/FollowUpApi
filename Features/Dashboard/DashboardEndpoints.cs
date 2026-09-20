using System.Security.Claims;
using FollowUpApi.Common;
using FollowUpApi.Domain.Interfaces;

namespace FollowUpApi.Features.Dashboard;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/companies/{companyId:guid}/dashboard/summary", HandleGetDashboardSummaryAsync).WithName("GetDashboardSummary").WithTags("Dashboard").RequireAuthorization()
            .Produces<ApiResponse<DashboardSummaryResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<DashboardSummaryResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<DashboardSummaryResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<DashboardSummaryResponse>>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleGetDashboardSummaryAsync(Guid companyId, ClaimsPrincipal currentUser, IDashboardManager manager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await manager.GetDashboardSummaryAsync(companyId, userId, cancellationToken);

        if (result.Success)
        {
            return Results.Ok(ApiResponse<DashboardSummaryResponse>.Ok(result.Data, result.Message));
        }

        var statusCode = result.Status switch
        {
            ServiceResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ServiceResultStatus.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Json(ApiResponse<DashboardSummaryResponse>.Fail(result.Message), statusCode: statusCode);
    }
}
