using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FollowUpApi.Common;
using FollowUpApi.DataContext.Entities;
using FollowUpApi.Domain.Interfaces;
using FollowUpApi.Features.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FollowUpApi.Domain.Managers;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;

        if (string.IsNullOrWhiteSpace(_jwtOptions.Issuer)
            || string.IsNullOrWhiteSpace(_jwtOptions.Audience)
            || string.IsNullOrWhiteSpace(_jwtOptions.Key))
        {
            throw new InvalidOperationException(
                "JWT issuer, audience, and key must be configured");
        }

        if (Encoding.UTF8.GetByteCount(_jwtOptions.Key) < 32)
        {
            throw new InvalidOperationException(
                "JWT key must contain at least 32 bytes");
        }

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.Key));

        _signingCredentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);
    }

    public AccessTokenResult CreateAccessToken(AppUser user)
    {
        var issuedOn = DateTime.UtcNow;
        var expiresOn = issuedOn.AddMinutes(_jwtOptions.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Name),
            new(ClaimTypes.Name, user.Name),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: issuedOn,
            expires: expiresOn,
            signingCredentials: _signingCredentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessTokenResult(accessToken, expiresOn);
    }
}
