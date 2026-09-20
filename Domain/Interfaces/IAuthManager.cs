using FollowUpApi.Common;
using FollowUpApi.Features.Authentication;

namespace FollowUpApi.Domain.Interfaces;

public interface IAuthManager
{
    Task<ServiceResult<LoginUserResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<ChangePasswordResponse>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<CurrentUserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ApiResponse<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}
