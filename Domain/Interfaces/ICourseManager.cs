using FollowUpApi.Common;
using FollowUpApi.Features.Courses;

namespace FollowUpApi.Domain.Interfaces;

public interface ICourseManager
{
    public Task<ServiceResult<CourseResponse>> CreateCourseAsync(Guid companyId, Guid requestedByUserId, CreateCourseRequest request, CancellationToken cancellationToken = default);

    public Task<ServiceResult<PaginationResponse<CourseResponse>>> GetCoursesAsync(Guid companyId, Guid requestedByUserId, GetCoursesRequest request, CancellationToken cancellationToken = default);

    public Task<ServiceResult<CourseResponse>> UpdateCourseAsync(Guid companyId, Guid courseId, Guid requestedByUserId, UpdateCourseRequest request, CancellationToken cancellationToken = default);

    public Task<ServiceResult<CourseResponse>> DeactivateCourseAsync(Guid companyId, Guid courseId, Guid requestedByUserId, CancellationToken cancellationToken = default);

}
