using FollowUpApi.Common;
using FollowUpApi.DataContext;
using FollowUpApi.DataContext.Entities;
using FollowUpApi.Domain.Interfaces;
using FollowUpApi.Domain.Managers;
using FollowUpApi.Features.Authentication;
using FollowUpApi.Features.CompanyManagement;
using FollowUpApi.Features.CompanyUsers;
using FollowUpApi.Features.Courses;
using FollowUpApi.Features.FollowUps;
using FollowUpApi.Features.Leads;
using FollowUpApi.Features.LeadSources;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<BearerSecurityRequirementTransformer>();
});

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

// Authentication
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing");

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer)
    || string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    throw new InvalidOperationException(
        "JWT issuer and audience must be configured");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Key)
    || Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
{
    throw new InvalidOperationException(
        "JWT key must be configured and contain at least 32 bytes");
}

builder.Services.Configure<JwtOptions>(jwtSection);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// Dependency Injection
builder.Services.AddTransient<ICompanyManager, CompanyManager>();
builder.Services.AddScoped<IUserManager, UserManager>();
builder.Services.AddScoped<ICompanyAccessService, CompanyAccessService>();
builder.Services.AddScoped<ICourseManager, CourseManager>();
builder.Services.AddScoped<ILeadSourceManager, LeadSourceManager>();
builder.Services.AddScoped<ILeadManager, LeadManager>();
builder.Services.AddScoped<IFollowUpManager, FollowUpManager>();
builder.Services.AddTransient<IDashboardManager, DashboardManager>();
builder.Services.AddScoped<IAuthManager, AuthManager>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

var app = builder.Build();

app.MapOpenApi();

// Scalar UI
app.MapScalarApiReference();

app.UseAuthentication();
app.UseAuthorization();

// Register Endpoints
app.MapCompanyOnboardEndpont();
app.MapLoginEndpoint();
app.MapCompanyUserEndpoints();
app.MapCourseEndpoints();
app.MapLeadSourceEndpoints();
app.MapLeadEndpoints();
app.MapFollowUpEndpoints();
app.Run();
