# FollowUpApi Login + JWT overlay

This overlay adds `POST /api/auth/login` to the existing .NET 10 API.

## What it does

- Accepts an email address or mobile number in `identification`.
- Verifies the ASP.NET Core Identity password hash created during company onboarding.
- Returns a signed JWT, user details, and all active company memberships.
- Keeps company and role out of the JWT because one user can belong to several companies.
- Uses the same generic response for an unknown user and a wrong password.

## Apply it

Copy the overlay files into the root of the existing `FollowUpApi` project, keeping the folder structure. The included `CompanyOnboardEndpoint.cs` also fixes the current failure path so it does not echo the submitted password.

The included project file adds:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.9" />
```

Merge the `Jwt` object from `appsettings.Jwt.example.json` into the project's `appsettings.json`. Do not put the signing key in `appsettings.json` or source control.

From the project folder, store a development signing key with user secrets:

```powershell
dotnet user-secrets init
$jwtKey = [Convert]::ToBase64String(
    [Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet user-secrets set "Jwt:Key" $jwtKey
```

Then restore and run:

```powershell
dotnet restore
dotnet run
```

## Scalar test

Send this body to `POST /api/auth/login`:

```json
{
  "identification": "owner@example.com",
  "password": "YourOnboardingPassword"
}
```

The successful response contains `data.access_token`. Protected endpoints should receive it in this header:

```text
Authorization: Bearer <access_token>
```

No database migration is required for this endpoint.
