using FollowUpApi.Common;
using FollowUpApi.DataContext;
using FollowUpApi.DataContext.Entities;
using FollowUpApi.Domain.Interfaces;
using FollowUpApi.Features.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FollowUpApi.Domain.Managers;

public sealed class AuthManager : IAuthManager
{
    private const string ActiveStatus = "Active";

    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthManager> _logger;

    public AuthManager(
        AppDbContext dbContext,
        IPasswordHasher<AppUser> passwordHasher,
        ITokenService tokenService,
        ILogger<AuthManager> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var identification = request.Identification?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(identification)
            || string.IsNullOrWhiteSpace(password))
        {
            return InvalidCredentials();
        }

        var isEmail = identification.Contains('@');
        var normalizedIdentification = isEmail
            ? identification.ToLowerInvariant()
            : NormalizeMobile(identification);

        var user = await _dbContext.AppUsers
            .FirstOrDefaultAsync(
                appUser =>
                    !appUser.IsDeleted
                    && (isEmail
                        ? EF.Functions.ILike(appUser.Email, normalizedIdentification)
                        : appUser.Mobile == normalizedIdentification),
                cancellationToken);

        if (user is null
            || !string.Equals(
                user.Status,
                ActiveStatus,
                StringComparison.OrdinalIgnoreCase))
        {
            return InvalidCredentials();
        }

        var passwordVerification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        if (passwordVerification == PasswordVerificationResult.Failed)
        {
            return InvalidCredentials();
        }

        if (passwordVerification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var companies = await _dbContext.LinkCompanyUsers
            .AsNoTracking()
            .Where(link =>
                !link.IsDeleted
                && link.UserId == user.Id
                && link.Status == ActiveStatus
                && link.Company != null
                && !link.Company.IsDeleted
                && link.Company.Status == ActiveStatus)
            .OrderByDescending(link => link.IsPrimaryOwner)
            .ThenBy(link => link.Company!.CompanyName)
            .Select(link => new LoginCompanyResponse
            {
                CompanyId = link.CompanyId,
                CompanyName = link.Company!.CompanyName,
                Role = link.Role,
                IsPrimaryOwner = link.IsPrimaryOwner
            })
            .ToListAsync(cancellationToken);

        var token = _tokenService.CreateAccessToken(user);

        var response = new LoginResponse
        {
            AccessToken = token.AccessToken,
            ExpiresOn = token.ExpiresOn,
            ExpiresInSeconds = Math.Max(
                0,
                (int)(token.ExpiresOn - DateTime.UtcNow).TotalSeconds),
            User = new LoginUserResponse
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Mobile = user.Mobile
            },
            Companies = companies
        };

        _logger.LogInformation(
            "User {UserId} logged in with {CompanyCount} company memberships",
            user.Id,
            companies.Count);

        return ApiResponse<LoginResponse>.Ok(response, "Login successful");
    }

    private static string NormalizeMobile(string mobile)
    {
        return new string(mobile.Where(char.IsDigit).ToArray());
    }

    private static ApiResponse<LoginResponse> InvalidCredentials()
    {
        return ApiResponse<LoginResponse>.Fail(
            "Invalid email/mobile number or password");
    }
}
