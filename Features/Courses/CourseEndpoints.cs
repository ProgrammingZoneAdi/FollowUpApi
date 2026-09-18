using FollowUpApi.Common;
using FollowUpApi.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FollowUpApi.Features.Courses;

public static class CourseEndpoints
{
    public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/companies/{companyId:guid}/courses", HandleCreateCourseAsync).WithName("CreateCourse").WithTags("Courses").RequireAuthorization()
            .Accepts<CreateCourseRequest>("application/json")
            .Produces<ApiResponse<CourseResponse>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<CourseResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<CourseResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<CourseResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<CourseResponse>>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse<CourseResponse>>(StatusCodes.Status500InternalServerError);

        app.MapGet("/api/companies/{companyId:guid}/courses", HandleGetCoursesAsync).WithName("GetCourses").WithTags("Courses").RequireAuthorization()
            .Produces<ApiResponse<PaginationResponse<CourseResponse>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<PaginationResponse<CourseResponse>>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<PaginationResponse<CourseResponse>>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<PaginationResponse<CourseResponse>>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<PaginationResponse<CourseResponse>>>(StatusCodes.Status500InternalServerError);

        app.MapPut("/api/companies/{companyId:guid}/courses/{courseId:guid}", HandleUpdateCourseAsync).WithName("UpdateCourse").WithTags("Courses").RequireAuthorization()
            .Accepts<UpdateCourseRequest>("application/json")
            .Produces<ApiResponse<CourseResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<CourseResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<CourseResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<CourseResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<CourseResponse>>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse<CourseResponse>>(StatusCodes.Status500InternalServerError);

        app.MapDelete("/api/companies/{companyId:guid}/courses/{courseId:guid}", HandleDeactivateCourseAsync).WithName("DeactivateCourse").WithTags("Courses").RequireAuthorization()
            .Produces<ApiResponse<CourseResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse<CourseResponse>>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse<CourseResponse>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<CourseResponse>>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse<CourseResponse>>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> HandleCreateCourseAsync(Guid companyId, ClaimsPrincipal currentUser, [FromBody] CreateCourseRequest request, ICourseManager courseManager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if(!Guid.TryParse(userIdValue, out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await courseManager.CreateCourseAsync(companyId, requestedByUserId, request, cancellationToken);

        if (result.Success)
        {
            var response = ApiResponse<CourseResponse>.Ok(result.Data, result.Message);

            return Results.Created($"/api/companies/{companyId}/courses/{result.Data!.CourseId}", response);
        }

        return MapError(result);
    }

    private static async Task<IResult> HandleGetCoursesAsync(Guid companyId, ClaimsPrincipal currentUser, [AsParameters] GetCoursesRequest request, ICourseManager courseManager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if(!Guid.TryParse(userIdValue, out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await courseManager.GetCoursesAsync(companyId, requestedByUserId, request, cancellationToken);

        if (result.Success)
        {
            var response = ApiResponse<PaginationResponse<CourseResponse>>.Ok(result.Data, result.Message);

            return Results.Ok(response);
        }

        return MapError(result);
    }

    private static async Task<IResult> HandleUpdateCourseAsync(Guid companyId, Guid courseId, ClaimsPrincipal currentUser, [FromBody] UpdateCourseRequest request, ICourseManager courseManager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if(!Guid.TryParse(userIdValue, out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await courseManager.UpdateCourseAsync(companyId, courseId, requestedByUserId, request, cancellationToken);

        if (result.Success)
        {
            var response = ApiResponse<CourseResponse>.Ok(result.Data, result.Message);

            return Results.Ok(response);
        }

        return MapError(result);
    }

    private static async Task<IResult> HandleDeactivateCourseAsync(Guid companyId, Guid courseId, ClaimsPrincipal currentUser, ICourseManager courseManager, CancellationToken cancellationToken)
    {
        var userIdValue = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);

        if(!Guid.TryParse(userIdValue, out var requestedByUserId))
        {
            return Results.Unauthorized();
        }

        var result = await courseManager.DeactivateCourseAsync(companyId, courseId, requestedByUserId, cancellationToken);

        if (result.Success)
        {
            var response = ApiResponse<CourseResponse>.Ok(result.Data, result.Message);
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
