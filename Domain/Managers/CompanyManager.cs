using FollowUpApi.Common;
using FollowUpApi.DataContext;
using FollowUpApi.DataContext.Entities;
using FollowUpApi.Domain.Interfaces;
using FollowUpApi.Features.CompanyManagement;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FollowUpApi.Domain.Managers;

public class CompanyManager : ICompanyManager
{

    private const string ActiveStatus = "Active";
    private const string OwnerRole = "Owner";

    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly ILogger<CompanyManager> _logger;
    public CompanyManager(AppDbContext dbContext,IPasswordHasher<AppUser> passwordHasher,ILogger<CompanyManager> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<ApiResponse<CompanyOnboardResponse>> OnboardCompanyAsync(CompanyOnboardRequest request,CancellationToken cancellationToken = default)
    {
        var companyName = request.CompanyName?.Trim() ?? string.Empty;
        var ownerName = request.OwnerName?.Trim() ?? string.Empty;
        var mobile = NormalizeMobile(request.Mobile ?? string.Empty);
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        var validationError = ValidateRequest(companyName, ownerName, mobile, email, password);

        if (validationError is not null)
        {
            return ApiResponse<CompanyOnboardResponse>.Fail(validationError);
        }

        var companyAlreadyExists = await _dbContext.Companies.AsNoTracking()
            .AnyAsync(company => !company.IsDeleted && EF.Functions.ILike(company.CompanyName, companyName) && company.Mobile == mobile, cancellationToken);

        if (companyAlreadyExists)
        {
            return ApiResponse<CompanyOnboardResponse>.Fail("Company already exists with the same name and mobile number");
        }

        var userAlreadyExists = await _dbContext.AppUsers.AsNoTracking()
            .AnyAsync(user => !user.IsDeleted && (user.Mobile == mobile || EF.Functions.ILike(user.Email, email)), cancellationToken);

        if (userAlreadyExists)
        {
            return ApiResponse<CompanyOnboardResponse>.Fail("An account already exists with the same mobile number or email");
        }

        await using var transaction =  await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var company = new Company
            {
                CompanyName = companyName,
                OwnerName = ownerName,
                Mobile = mobile,
                Email = email,
                Status = ActiveStatus
            };

            var ownerUser = new AppUser
            {
                Name = ownerName,
                Mobile = mobile,
                Email = email,
                Status = ActiveStatus
            };

            ownerUser.PasswordHash = _passwordHasher.HashPassword(ownerUser, password);

            var companyUser = new LinkCompanyUser
            {
                CompanyId = company.Id,
                UserId = ownerUser.Id,
                Role = OwnerRole,
                IsPrimaryOwner = true,
                Status = ActiveStatus
            };

            await _dbContext.Companies.AddAsync(company, cancellationToken);

            await _dbContext.AppUsers.AddAsync(ownerUser, cancellationToken);

            await _dbContext.LinkCompanyUsers.AddAsync(companyUser, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var response = new CompanyOnboardResponse
            {
                CompanyId = company.Id,
                UserId = ownerUser.Id,
                LinkCompanyUserId = companyUser.Id,
                Role = companyUser.Role
            };

            return ApiResponse<CompanyOnboardResponse>.Ok( response, "Company onboarded successfully");
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(CancellationToken.None);

            _logger.LogWarning( exception, "Database update failed while onboarding company {CompanyName}",companyName);

            return ApiResponse<CompanyOnboardResponse>.Fail("Company could not be onboarded because the data conflicts with an existing record");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(CancellationToken.None);

            _logger.LogError(exception, "Unexpected error while onboarding company {CompanyName}",  companyName);

            return ApiResponse<CompanyOnboardResponse>.Fail("An unexpected error occurred while onboarding the company");
        }
    }

    private static string NormalizeMobile(string mobile)
    {
        return new string(mobile.Where(char.IsDigit).ToArray());
    }

    private static string? ValidateRequest(string companyName, string ownerName, string mobile, string email , string password)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            return "Company name is required";

        if (companyName.Length > 200)
            return "Company name cannot exceed 200 characters";


        if (string.IsNullOrWhiteSpace(ownerName))
            return "Owner name is required";

        if (ownerName.Length > 150)
            return "Owner name cannot exceed 150 characters";

        if (mobile.Length is < 10 or > 15)
            return "Mobile number must contain between 10 and 15 digits";

        if (string.IsNullOrWhiteSpace(email))
            return "Email is required";

        if(email.Length > 254 || !new EmailAddressAttribute().IsValid(email))
        {
            return "A valid email address is requied";
        }

        if (string.IsNullOrWhiteSpace(password))
            return "Password is required";

        if (password.Length < 8)
            return "Password must contain at least 8 characters";

        if(!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || !password.Any(character => !char.IsLetterOrDigit(character)))
        {
            return "Password must include uppercase, lowercase, number and special character";
        }

        return null;

    }

}
