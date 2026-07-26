using System.ComponentModel.DataAnnotations;
using FollowUpApi.Common;
using FollowUpApi.DataContext;
using FollowUpApi.DataContext.Entities;
using FollowUpApi.Domain.Interfaces;
using FollowUpApi.Features.CompanyUsers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FollowUpApi.Domain.Managers;

public sealed class UserManager : IUserManager
{
    private const string ActiveStatus = "Active";

    private static readonly string[] UserManagementRoles =
    [
        "Owner",
        "Admin"
    ];

    private static readonly Dictionary<string, string> AssignableRoles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Admin"] = "Admin",
            ["Counsellor"] = "Counsellor",
            ["Staff"] = "Staff"
        };

    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly ICompanyAccessService _companyAccessService;
    private readonly ILogger<UserManager> _logger;

    public UserManager(
        AppDbContext dbContext,
        IPasswordHasher<AppUser> passwordHasher,
        ICompanyAccessService companyAccessService,
        ILogger<UserManager> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _companyAccessService = companyAccessService;
        _logger = logger;
    }

    public async Task<ServiceResult<AddCompanyUserResponse>>
        AddCompanyUserAsync(
            Guid companyId,
            Guid requestedByUserId,
            AddCompanyUserRequest request,
            CancellationToken cancellationToken = default)
    {
        // First check whether the logged-in user is Owner/Admin.
        var access = await _companyAccessService.CheckAccessAsync(
            companyId,
            requestedByUserId,
            UserManagementRoles,
            cancellationToken);

        if (access.Status == CompanyAccessStatus.CompanyNotFound)
        {
            return ServiceResult<AddCompanyUserResponse>.NotFound(
                "Company was not found");
        }

        if (access.Status == CompanyAccessStatus.Forbidden)
        {
            return ServiceResult<AddCompanyUserResponse>.Forbidden(
                "Only a company owner or admin can add team members");
        }

        var name = request.Name?.Trim() ?? string.Empty;

        var mobile = NormalizeMobile(
            request.Mobile ?? string.Empty);

        var email = request.Email?
            .Trim()
            .ToLowerInvariant() ?? string.Empty;

        var requestedRole = request.Role?.Trim() ?? string.Empty;

        var validationError = ValidateProfile(
            name,
            mobile,
            email);

        if (validationError is not null)
        {
            return ServiceResult<AddCompanyUserResponse>
                .ValidationError(validationError);
        }

        if (!AssignableRoles.TryGetValue(
            requestedRole,
            out var role))
        {
            return ServiceResult<AddCompanyUserResponse>
                .ValidationError(
                    "Role must be Admin, Counsellor, or Staff");
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            // Search globally because AppUser does not belong
            // directly to only one company.
            var matchingUsers = await _dbContext.AppUsers
                .Where(user =>
                    !user.IsDeleted
                    && (
                        EF.Functions.ILike(user.Email, email)
                        || user.Mobile == mobile
                    ))
                .ToListAsync(cancellationToken);

            var emailMatches = matchingUsers
                .Where(user =>
                    string.Equals(
                        user.Email,
                        email,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            var mobileMatches = matchingUsers
                .Where(user => user.Mobile == mobile)
                .ToList();

            if (emailMatches.Count > 1
                || mobileMatches.Count > 1)
            {
                return await RollbackConflictAsync(
                    transaction,
                    "Existing user data contains duplicate email or mobile records");
            }

            var emailUser = emailMatches.FirstOrDefault();
            var mobileUser = mobileMatches.FirstOrDefault();

            // Email and mobile must not belong to two different accounts.
            if (emailUser is not null
                && mobileUser is not null
                && emailUser.Id != mobileUser.Id)
            {
                return await RollbackConflictAsync(
                    transaction,
                    "Email and mobile number belong to different user accounts");
            }

            var appUser = emailUser ?? mobileUser;
            var isNewUser = appUser is null;

            if (appUser is not null)
            {
                // Existing user is linked only when both contact
                // details match the same account.
                var identityMatches =
                    string.Equals(
                        appUser.Email,
                        email,
                        StringComparison.OrdinalIgnoreCase)
                    && appUser.Mobile == mobile;

                if (!identityMatches)
                {
                    return await RollbackConflictAsync(
                        transaction,
                        "An account already exists with one of these contact details");
                }

                if (!string.Equals(
                    appUser.Status,
                    ActiveStatus,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return await RollbackConflictAsync(
                        transaction,
                        "The existing user account is not active");
                }

                // Existing user's password and global profile
                // are deliberately not changed here.
            }
            else
            {
                var passwordError =
                    ValidatePassword(request.Password);

                if (passwordError is not null)
                {
                    await transaction.RollbackAsync(
                        CancellationToken.None);

                    return ServiceResult<AddCompanyUserResponse>
                        .ValidationError(passwordError);
                }

                appUser = new AppUser
                {
                    Name = name,
                    Mobile = mobile,
                    Email = email,
                    Status = ActiveStatus,
                    CreatedBy = requestedByUserId
                };

                appUser.PasswordHash =
                    _passwordHasher.HashPassword(
                        appUser,
                        request.Password!);

                await _dbContext.AppUsers.AddAsync(
                    appUser,
                    cancellationToken);
            }

            // Include deleted/inactive membership because we may
            // reactivate it instead of creating a duplicate row.
            var membership =
                await _dbContext.LinkCompanyUsers
                    .FirstOrDefaultAsync(
                        link =>
                            link.CompanyId == companyId
                            && link.UserId == appUser.Id,
                        cancellationToken);

            var wasReactivated = false;

            if (membership is not null)
            {
                if (membership.IsPrimaryOwner)
                {
                    return await RollbackConflictAsync(
                        transaction,
                        "The primary owner membership cannot be changed here");
                }

                if (!membership.IsDeleted
                    && string.Equals(
                        membership.Status,
                        ActiveStatus,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return await RollbackConflictAsync(
                        transaction,
                        "User is already a member of this company");
                }

                membership.Role = role;
                membership.Status = ActiveStatus;
                membership.IsDeleted = false;
                membership.UpdatedOn = DateTime.UtcNow;
                membership.UpdatedBy = requestedByUserId;

                wasReactivated = true;
            }
            else
            {
                membership = new LinkCompanyUser
                {
                    CompanyId = companyId,
                    UserId = appUser.Id,
                    Role = role,
                    IsPrimaryOwner = false,
                    Status = ActiveStatus,
                    CreatedBy = requestedByUserId
                };

                await _dbContext.LinkCompanyUsers.AddAsync(
                    membership,
                    cancellationToken);
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            var response = new AddCompanyUserResponse
            {
                LinkCompanyUserId = membership.Id,
                CompanyId = companyId,
                UserId = appUser.Id,
                Name = appUser.Name,
                Mobile = appUser.Mobile,
                Email = appUser.Email,
                Role = membership.Role,
                Status = membership.Status,
                IsNewUser = isNewUser
            };

            _logger.LogInformation(
                "User {UserId} added to company {CompanyId} " +
                "with role {Role} by {RequestedByUserId}",
                appUser.Id,
                companyId,
                membership.Role,
                requestedByUserId);

            string message;

            if (wasReactivated)
            {
                message = "Company user reactivated successfully";
            }
            else if (isNewUser)
            {
                message = "Company user created successfully";
            }
            else
            {
                message =
                    "Existing user linked to company successfully";
            }

            return ServiceResult<AddCompanyUserResponse>.Ok(
                response,
                message);
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            _logger.LogWarning(
                exception,
                "Database conflict while adding a user " +
                "to company {CompanyId}",
                companyId);

            return ServiceResult<AddCompanyUserResponse>
                .Conflict(
                    "Company user could not be added because " +
                    "the data conflicts with an existing record");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            _logger.LogError(
                exception,
                "Unexpected error while adding a user " +
                "to company {CompanyId}",
                companyId);

            return ServiceResult<AddCompanyUserResponse>
                .Error(
                    "An unexpected error occurred while " +
                    "adding the company user");
        }
    }

    private static async Task<ServiceResult<AddCompanyUserResponse>>
        RollbackConflictAsync(
            IDbContextTransaction transaction,
            string message)
    {
        await transaction.RollbackAsync(
            CancellationToken.None);

        return ServiceResult<AddCompanyUserResponse>
            .Conflict(message);
    }

    private static string NormalizeMobile(string mobile)
    {
        return new string(
            mobile.Where(char.IsDigit).ToArray());
    }

    private static string? ValidateProfile(
        string name,
        string mobile,
        string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Name is required";

        if (name.Length > 150)
            return "Name cannot exceed 150 characters";

        if (mobile.Length is < 10 or > 15)
        {
            return "Mobile number must contain between 10 and 15 digits";
        }

        if (string.IsNullOrWhiteSpace(email)
            || email.Length > 254
            || !new EmailAddressAttribute().IsValid(email))
        {
            return "A valid email address is required";
        }

        return null;
    }

    private static string? ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return "Password is required when creating a new user";
        }

        if (password.Length < 8)
        {
            return "Password must contain at least 8 characters";
        }

        var hasUppercase = password.Any(char.IsUpper);
        var hasLowercase = password.Any(char.IsLower);
        var hasNumber = password.Any(char.IsDigit);

        var hasSpecialCharacter =
            password.Any(character =>
                !char.IsLetterOrDigit(character));

        if (!hasUppercase
            || !hasLowercase
            || !hasNumber
            || !hasSpecialCharacter)
        {
            return "Password must include uppercase, lowercase, " +
                   "number and special character";
        }

        return null;
    }
}