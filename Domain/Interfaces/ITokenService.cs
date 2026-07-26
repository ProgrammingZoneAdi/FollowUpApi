using FollowUpApi.DataContext.Entities;
using FollowUpApi.Features.Authentication;

namespace FollowUpApi.Domain.Interfaces;

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(AppUser user);
}
