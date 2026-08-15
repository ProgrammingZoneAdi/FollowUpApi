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
    private const string InactiveStatus = "Inactive";

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

    public async Task<ServiceResult<PaginationResponse<CompanyUserListItemResponse>>> GetCompanyUsersAsync(Guid companyId, Guid requestedByUserId, GetCompanyUsersRequest request, CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, Array.Empty<string>(), cancellationToken);

        if(access.Status == CompanyAccessStatus.CompanyNotFound)
        {
            return ServiceResult<PaginationResponse<CompanyUserListItemResponse>>.NotFound("Company was not found");
        }

        if(access.Status == CompanyAccessStatus.Forbidden)
        {
            return ServiceResult<PaginationResponse<CompanyUserListItemResponse>>.Forbidden("You are not an active member of this company");
        }

        var pageNumber = request.PageNumber;
        var pageSize = request.PageSize;
        var search = request.Search?.Trim();
        var requestedRole = request.Role?.Trim();
        var requestedStatus = request.Status?.Trim();

        if (pageNumber < 1)
        {
            return ServiceResult<PaginationResponse<CompanyUserListItemResponse>>.ValidationError("Page Number must be greater than zero");
        }

        if(pageSize is < 1 or > 100)
        {
            return ServiceResult<PaginationResponse<CompanyUserListItemResponse>>.ValidationError("Page size must be between 1 and 100");
        }

        if(!string.IsNullOrWhiteSpace(search) && search.Length > 100)
        {
            return ServiceResult<PaginationResponse<CompanyUserListItemResponse>>.ValidationError("Search text cannot exceed 100 characters");
        }

        if((long)(pageNumber - 1) * pageSize > int.MaxValue)
        {
            return ServiceResult<PaginationResponse<CompanyUserListItemResponse>>.ValidationError("Requested page is too large");
        }

        string? roleFilter = null;

        if(!string.IsNullOrWhiteSpace(requestedRole) && !string.Equals(requestedRole, "All", StringComparison.OrdinalIgnoreCase))
        {
            var allowedRole = new[] { "Owner", "Admin", "Counsellor", "Staff" }.FirstOrDefault(role => string.Equals(role, requestedRole, StringComparison.OrdinalIgnoreCase));

            if(allowedRole is null)
            {
                return ServiceResult<PaginationResponse<CompanyUserListItemResponse>>.ValidationError("Role must be Owner, Admin, " + "Counsellor, Staff, or All");
            }

            roleFilter = allowedRole;
        }

        string? statusFilter;

        if (string.IsNullOrWhiteSpace(requestedStatus))
        {
            statusFilter = ActiveStatus;
        }
        else if (string.Equals(requestedStatus, "All", StringComparison.OrdinalIgnoreCase))
        {
            statusFilter = null;
        }
        else if(string.Equals(requestedStatus, "Active", StringComparison.OrdinalIgnoreCase))
        {
            statusFilter = "Active";
        }
        else if(string.Equals(requestedStatus, "Inactive", StringComparison.OrdinalIgnoreCase))
        {
            statusFilter = "Inactive";
        }
        else
        {
            return ServiceResult<PaginationResponse<CompanyUserListItemResponse>>.ValidationError("Status must be Active, Inactive, or All");
        }

        try
        {
            var query = _dbContext.LinkCompanyUsers.AsNoTracking().Where(link => !link.IsDeleted && link.CompanyId == companyId && link.User != null && !link.User.IsDeleted);

            if(roleFilter is not null)
            {
                query = query.Where(link => link.Role == roleFilter);
            }

            if (statusFilter is not null)
            {
                query = query.Where(link => link.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchPattern = $"%{search}%";

                query = query.Where(link => EF.Functions.ILike(link.User!.Name, searchPattern) || EF.Functions.ILike(link.User.Email, searchPattern) || EF.Functions.ILike(link.User.Mobile, searchPattern));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            // owner will appear first
            var rows = await query.OrderByDescending(link => link.IsPrimaryOwner).ThenBy(link => link.User!.Name)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize).Select(link => new CompanyUserListItemResponse
                {
                    LinkCompanyUserId = link.Id,
                    UserId = link.UserId,
                    Name = link.User!.Name,
                    Mobile = link.User.Mobile,
                    Email = link.User.Email,
                    Role = link.Role,
                    Status = link.Status,
                    IsPrimaryOwner = link.IsPrimaryOwner,
                    JoinedOn = link.CreatedOn
                }).ToListAsync(cancellationToken);

            var response = new PaginationResponse<CompanyUserListItemResponse>
            {
                Rows = rows,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ServiceResult<PaginationResponse<CompanyUserListItemResponse>>.Ok(response, "Company users retrieved successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch(Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while retrieving users " + "for company {CompanyId}", companyId);

            return ServiceResult<PaginationResponse<CompanyUserListItemResponse>>.Error("An unexpected error occured while " + "retrieving company users");
        }
        
    }

    public async Task<ServiceResult<CompanyUserListItemResponse>> UpdateCompanyUserRoleAsync(Guid companyId, Guid targetUserId, Guid requestedByUserId, UpdateCompanyUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, UserManagementRoles, cancellationToken);

        if(access.Status == CompanyAccessStatus.CompanyNotFound)
        {
            return ServiceResult<CompanyUserListItemResponse>.NotFound("Company was not found");
        }

        if(access.Status == CompanyAccessStatus.Forbidden)
        {
            return ServiceResult<CompanyUserListItemResponse>.Forbidden("Only a company owner or admin " + "can update member roles");
        }

        // Avoid accidental self - demotion.
        if(targetUserId == requestedByUserId)
        {
            return ServiceResult<CompanyUserListItemResponse>.Conflict("You cannot change your own company role");
        }

        var requestedRole = request.Role?.Trim() ?? string.Empty;

        // AssignableRoles already exists in UserManager.

        if(!AssignableRoles.TryGetValue(requestedRole, out var role))
        {
            return ServiceResult<CompanyUserListItemResponse>.ValidationError("Role must be Admin, Counsellor, or staff");
        }

        try
        {
            var membership = await _dbContext.LinkCompanyUsers.Include(link => link.User)
                .FirstOrDefaultAsync(link => !link.IsDeleted && link.CompanyId == companyId && link.UserId == targetUserId, cancellationToken);

            if(membership is null || membership.User is null || membership.User.IsDeleted)
            {
                return ServiceResult<CompanyUserListItemResponse>.NotFound("Company user was not found");
            }

            if (membership.IsPrimaryOwner)
            {
                return ServiceResult<CompanyUserListItemResponse>.Conflict("The primary owner's role cannot be changed");
            }

            if (!string.Equals(membership.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<CompanyUserListItemResponse>.Conflict("An inactive company user's role cannot be changed");
            }

            if (string.Equals(membership.Role,role, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<CompanyUserListItemResponse>
                    .Ok(MapCompanyUser(membership),"Company user already has this role");
            }

            var oldRole = membership.Role;

            membership.Role = role;
            membership.UpdatedOn = DateTime.UtcNow;
            membership.UpdatedBy = requestedByUserId;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Company user {TargetUserId} role changed " + "from {OldRole} to {NewRole} in company " + "{CompanyId} by {RequestedByUserId}",
                targetUserId, oldRole, role, companyId, requestedByUserId);

            return ServiceResult<CompanyUserListItemResponse>.Ok(MapCompanyUser(membership), "Company user role updated successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch(DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Database conflict while updating user " + "{TargetUserId} role in company {CompanyId}", targetUserId, companyId);

            return ServiceResult<CompanyUserListItemResponse>.Conflict("Company user role could not updated " + "because the data conflicts with an " + "existing record");
        }
        catch(Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while updating user " + "{TargetUserId} role in company {CompanyId}", targetUserId, companyId);

            return ServiceResult<CompanyUserListItemResponse>.Error("An unexpected error occured while " + "updating the company user role");
        }

    }


    public async Task<ServiceResult<CompanyUserListItemResponse>> DeactivateCompanyUserAsync(Guid companyId, Guid targetUserId, Guid requestedByUserId, CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.CheckAccessAsync(companyId, requestedByUserId, UserManagementRoles, cancellationToken);

        if(access.Status == CompanyAccessStatus.CompanyNotFound)
        {
            return ServiceResult<CompanyUserListItemResponse>.NotFound("Company was not found");
        }

        if(access.Status == CompanyAccessStatus.Forbidden)
        {
            return ServiceResult<CompanyUserListItemResponse>.Forbidden("Only a company owner or admin " + "can deactivate team members");
        }

        // prevent accidental self - lockout.

        if(targetUserId == requestedByUserId)
        {
            return ServiceResult<CompanyUserListItemResponse>.Conflict("You cannot deactivate your own " + "company membership");
        }

        try
        {
            var membership = await _dbContext.LinkCompanyUsers.Include(link => link.User).FirstOrDefaultAsync(link => !link.IsDeleted && link.CompanyId == companyId && link.UserId == targetUserId, cancellationToken);

            if(membership is null || membership.User is null || membership.User.IsDeleted)
            {
                return ServiceResult<CompanyUserListItemResponse>.NotFound("Company user was not found");
            }

            // Company must always retain its primary owner.

            if (membership.IsPrimaryOwner)
            {
                return ServiceResult<CompanyUserListItemResponse>.Conflict("The primary owner cannot be deactivated");
            }

            // DELETE remains idempotent.
            if(string.Equals(membership.Status, InactiveStatus, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<CompanyUserListItemResponse>.Ok(MapCompanyUser(membership), "Company user is already inactive");
            }

            // Active leads must not become assigned to an inactive company member.

            var assignedActiveLeadCount = await _dbContext.Leads.AsNoTracking().CountAsync(lead => !lead.IsDeleted && lead.CompanyId == companyId && lead.AssignedToUserId == targetUserId && lead.Status != "Won" && lead.Status != "Lost", cancellationToken);


            if(assignedActiveLeadCount > 0)
            {
                return ServiceResult<CompanyUserListItemResponse>.Conflict($"Reassing {assignedActiveLeadCount} " + "active lead(s) before deactivating " + "this company user");
            }

            membership.Status = InactiveStatus;
            membership.UpdatedOn = DateTime.UtcNow;
            membership.UpdatedBy = requestedByUserId;

            // Do not change AppUser.Status. Do not set membership.IsDeleted = true.

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Company user {TargetUserId} deactivated " + "in company {CompanyId} by " + "{RequestedByUserId}", targetUserId, companyId, requestedByUserId);

            return ServiceResult<CompanyUserListItemResponse>.Ok(MapCompanyUser(membership), "Company user deactivated successfully");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch(DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Database conflict when deactivating user " + "{TargetUserId} in company {CompanyId}", targetUserId, companyId);

            return ServiceResult<CompanyUserListItemResponse>.Conflict("Company user could not be deactivated " + "because the data conflicts with an " + "existing record");
        }
        catch(Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while deactivating user " + "{TargetUserId} in company {CompanyId}", targetUserId, companyId);

            return ServiceResult<CompanyUserListItemResponse>.Error("An unexpected error occured while " + "deactivating the company user");
        }
    }
    private static CompanyUserListItemResponse MapCompanyUser(LinkCompanyUser membership)
    {
        return new CompanyUserListItemResponse
        {
            LinkCompanyUserId = membership.Id,
            UserId = membership.UserId,
            Name = membership.User!.Name,
            Mobile = membership.User.Mobile,
            Email = membership.User.Email,
            Role = membership.Role,
            Status = membership.Status,
            IsPrimaryOwner = membership.IsPrimaryOwner,
            JoinedOn = membership.CreatedOn
        };
    }

    private static async Task<ServiceResult<AddCompanyUserResponse>> RollbackConflictAsync( IDbContextTransaction transaction,  string message)
    {
        await transaction.RollbackAsync(CancellationToken.None);

        return ServiceResult<AddCompanyUserResponse>.Conflict(message);
    }

    private static string NormalizeMobile(string mobile)
    {
        return new string(mobile.Where(char.IsDigit).ToArray());
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