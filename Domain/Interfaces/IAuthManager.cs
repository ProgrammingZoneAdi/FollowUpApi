using FollowUpApi.Common;
using FollowUpApi.Features.Authentication;

namespace FollowUpApi.Domain.Interfaces;

public interface IAuthManager
{
    Task<ApiResponse<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}
