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

    public async Task<ServiceResult<CurrentUserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _dbContext.AppUsers
                .AsNoTracking()
                .Where(user => user.Id == userId && !user.IsDeleted && user.Status == ActiveStatus)
                .Select(user => new CurrentUserResponse
                {
                    User = new LoginUserResponse
                    {
                        UserId = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        Mobile = user.Mobile
                    },
                    Companies = _dbContext.LinkCompanyUsers
                        .Where(link => link.UserId == user.Id
                            && !link.IsDeleted
                            && link.Status == ActiveStatus
                            && link.Company != null
                            && !link.Company.IsDeleted
                            && link.Company.Status == ActiveStatus)
                        .OrderByDescending(link => link.IsPrimaryOwner)
                        .ThenBy(link => link.Company!.CompanyName)
                        .ThenBy(link => link.CompanyId)
                        .Select(link => new LoginCompanyResponse
                        {
                            CompanyId = link.CompanyId,
                            CompanyName = link.Company!.CompanyName,
                            Role = link.Role,
                            IsPrimaryOwner = link.IsPrimaryOwner
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (response is null)
            {
                return ServiceResult<CurrentUserResponse>.NotFound("User account is unavailable. Please log in again.");
            }

            return ServiceResult<CurrentUserResponse>.Ok(response, "Current user retrieved successfully");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to retrieve current user {UserId}", userId);
            return ServiceResult<CurrentUserResponse>.Error("Unable to retrieve current user. Please try again.");
        }
    }

    public async Task<ServiceResult<ChangePasswordResponse>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var currentPassword = request.CurrentPassword ?? string.Empty;
        var newPassword = request.NewPassword ?? string.Empty;

        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            return ServiceResult<ChangePasswordResponse>.ValidationError("Current password and new password are required");
        }

        if (newPassword.Length < 8 || newPassword.Length > 128)
        {
            return ServiceResult<ChangePasswordResponse>.ValidationError("New password must contain between 8 and 128 characters");
        }

        if (!newPassword.Any(char.IsUpper) || !newPassword.Any(char.IsLower)
            || !newPassword.Any(char.IsDigit) || !newPassword.Any(character => !char.IsLetterOrDigit(character)))
        {
            return ServiceResult<ChangePasswordResponse>.ValidationError("New password must include uppercase, lowercase, number and special character");
        }

        if (newPassword == currentPassword)
        {
            return ServiceResult<ChangePasswordResponse>.ValidationError("New password must be different from the current password");
        }

        try
        {
            var user = await _dbContext.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.Id == userId && !user.IsDeleted && user.Status == ActiveStatus, cancellationToken);

            if (user is null)
            {
                return ServiceResult<ChangePasswordResponse>.NotFound("User account is unavailable. Please log in again.");
            }

            if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
            {
                return ServiceResult<ChangePasswordResponse>.ValidationError("Current password is incorrect");
            }

            var newPasswordHash = _passwordHasher.HashPassword(user, newPassword);
            var updatedOn = DateTime.UtcNow;

            // Update only if the verified password and active account state are unchanged.
            var affectedRows = await _dbContext.AppUsers
                .Where(account => account.Id == userId && !account.IsDeleted
                    && account.Status == ActiveStatus && account.PasswordHash == user.PasswordHash)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(account => account.PasswordHash, newPasswordHash)
                    .SetProperty(account => account.UpdatedOn, updatedOn)
                    .SetProperty(account => account.UpdatedBy, userId), cancellationToken);

            if (affectedRows == 0)
            {
                return ServiceResult<ChangePasswordResponse>.Conflict("Account changed during this request. Please log in again and retry.");
            }

            return ServiceResult<ChangePasswordResponse>.Ok(new ChangePasswordResponse
            {
                UserId = userId,
                UpdatedOn = updatedOn
            }, "Password changed successfully");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to change password for user {UserId}", userId);
            return ServiceResult<ChangePasswordResponse>.Error("Unable to change password. Please try again.");
        }
    }

    public async Task<ServiceResult<LoginUserResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name) || name.Length > 150)
        {
            return ServiceResult<LoginUserResponse>.ValidationError("Name is required and cannot exceed 150 characters");
        }

        try
        {
            var user = await _dbContext.AppUsers
                .FirstOrDefaultAsync(user => user.Id == userId && !user.IsDeleted && user.Status == ActiveStatus, cancellationToken);

            if (user is null)
            {
                return ServiceResult<LoginUserResponse>.NotFound("User account is unavailable. Please log in again.");
            }

            user.Name = name;
            user.UpdatedOn = DateTime.UtcNow;
            user.UpdatedBy = userId;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult<LoginUserResponse>.Ok(new LoginUserResponse
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Mobile = user.Mobile
            }, "Profile updated successfully");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to update profile for user {UserId}", userId);
            return ServiceResult<LoginUserResponse>.Error("Unable to update profile. Please try again.");
        }
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
