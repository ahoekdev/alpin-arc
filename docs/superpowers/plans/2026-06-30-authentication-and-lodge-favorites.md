# Authentication And Lodge Favorites Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add first-party user accounts with confirmed email login, local Mailpit email delivery, and private per-user lodge favorites rendered in the Next.js app.

**Architecture:** ASP.NET Core Identity owns user accounts, password hashing, email confirmation tokens, lockout, and cookie sign-in. The API exposes custom controller endpoints for auth and `/api/me/lodge-favorites`, while Next.js remains the browser-facing same-origin front door and rewrites `/api/*` to the API locally. Orval continues to generate typed clients, but all generated HTTP calls go through one shared fetcher that handles same-origin requests, credentials, CSRF headers, and problem responses.

**Tech Stack:** ASP.NET Core MVC on .NET 10, ASP.NET Core Identity, EF Core 10, Npgsql/PostgreSQL, Mailpit, OpenAPI/NSwag, Next.js 16 App Router, Orval fetch client, TypeScript, React client components for interactive favorite toggling.

---

## Decisions Captured

- Authentication is required for favorites.
- Use ASP.NET Core Identity with cookie auth.
- Use first-party email/password accounts stored in the existing PostgreSQL database.
- Keep future external/social login compatibility by using the standard Identity schema and `ApplicationUser : IdentityUser`.
- Require email confirmation before login.
- Public registration is open to anyone.
- Use email only for user identity in v1; no display name or profile.
- Keep all existing public catalog browsing anonymous.
- Favorites are private to the signed-in user.
- Hide the favorite button when there is no logged-in session.
- `/favorites` is protected and redirects logged-out users to `/login?returnUrl=/favorites`.
- Put the favorite toggle only on the lodge detail page in v1.
- Include a `/favorites` page that lists the current user’s saved lodges.
- Keep public lodge endpoints unchanged; expose favorite state through separate authenticated `/api/me/lodge-favorites...` endpoints.
- `/api/me/lodge-favorites` returns lodge summaries, not just IDs.
- Favorite list is sorted by lodge name ascending in the API.
- No pagination for favorites in v1.
- Favorite create/delete operations are idempotent.
- Favoriting a nonexistent lodge returns `404`.
- Use custom `/api/auth/*` controllers over Identity internals.
- Confirmation and reset email links land on Next.js pages first, then those pages call API endpoints.
- Registration does not sign users in; it shows a generic check-email state.
- Include forgot-password, reset-password, and resend-confirmation flows.
- Defer change-password, change-email, profile editing, and account settings.
- Avoid account enumeration:
  - registration returns generic success for existing confirmed/unconfirmed emails
  - forgot password returns generic success
  - resend confirmation returns generic success
  - login uses one generic failure response for wrong credentials and unconfirmed accounts
- Enable lockout for password sign-in.
- Use Identity default password rules.
- Include a Remember me checkbox.
- Persistent login lasts 14 days.
- Add CSRF protection for cookie-authenticated mutating requests.
- Add Mailpit to local Docker Compose.
- Use Next.js as the local and production browser-facing origin, with `/api/*` proxied to ASP.NET Core.
- Change Orval to use one shared fetcher/mutator for public and authenticated API calls.

## Local Next.js Docs Read Before Planning

Repo instruction says Next.js docs under `apps/web/node_modules/next/dist/docs/` are the source of truth before any Next.js work. The relevant local docs read for this plan:

- `apps/web/node_modules/next/dist/docs/02-pages/04-api-reference/04-config/01-next-config-js/rewrites.md`
- `apps/web/node_modules/next/dist/docs/01-app/01-getting-started/06-fetching-data.md`
- `apps/web/node_modules/next/dist/docs/01-app/01-getting-started/07-mutating-data.md`
- `apps/web/node_modules/next/dist/docs/01-app/01-getting-started/05-server-and-client-components.md`
- `apps/web/node_modules/next/dist/docs/01-app/01-getting-started/04-linking-and-navigating.md`

Implementation notes from those docs:

- App Router pages/layouts are Server Components by default and can fetch data on the server.
- Client Components should be isolated to interactive UI like the favorite toggle and form state.
- Server Components should fetch current auth/favorite state for initial render to avoid showing logged-out UI incorrectly.
- Forms and mutations must verify auth/authorization on the server side; client UI is not a security boundary.
- Dynamic authenticated pages may block on request-time data; add loading UI where the app already follows that pattern.
- Use `next.config.ts` rewrites to proxy same-origin `/api/:path*` traffic in local development.

## Current Code Context

- API entry point: `apps/api/Program.cs`
  - Currently calls `UseAuthorization()` but does not register authentication.
  - Uses controllers, OpenAPI, NSwag Swagger UI, EF Core Npgsql, and demo seeding in development.
- EF context: `apps/api/Data/ApplicationDbContext.cs`
  - Currently derives from `DbContext`.
  - Contains `Lodges`, `Stages`, `Tours`, `TourVariants`, and `TourVariantStages`.
- Existing API controllers:
  - `apps/api/Controllers/LodgesController.cs`
  - `apps/api/Controllers/StagesController.cs`
  - `apps/api/Controllers/ToursController.cs`
  - `apps/api/Controllers/TourVariantsController.cs`
- Existing service pattern:
  - Service interfaces and implementations live in `apps/api/Services`.
  - Controllers call services and convert `ServiceResult` through `ControllerResultExtensions`.
- Existing web app:
  - Next.js App Router under `apps/web/app`.
  - Global layout renders `components/SiteHeader.tsx`.
  - Lodge detail page is `apps/web/app/lodges/[id]/page.tsx`.
  - Public API clients are generated under `apps/web/lib/api/generated/orval`.
- Existing Orval config: `apps/web/orval.config.ts`
  - Generates fetch clients from `/openapi/v1.json`.
  - Uses `getApiBaseUrl()`.
- Existing Docker Compose:
  - Only PostgreSQL service exists.

## API Contract

### Auth Endpoints

- `GET /api/auth/csrf`
  - Anonymous.
  - Ensures the readable antiforgery cookie is set.
  - Returns `204 No Content`.
- `GET /api/auth/me`
  - Anonymous.
  - Returns `CurrentUserDto`.
  - Logged out response: `200 { "isAuthenticated": false, "email": null }`.
  - Logged in response: `200 { "isAuthenticated": true, "email": "user@example.com" }`.
- `POST /api/auth/register`
  - Anonymous, CSRF-protected.
  - Body: `RegisterRequestDto`.
  - Returns generic `204 No Content` when the request is accepted, including duplicate-account cases.
  - Returns `400 ValidationProblemDetails` for invalid email/password format.
- `POST /api/auth/login`
  - Anonymous, CSRF-protected.
  - Body: `LoginRequestDto`.
  - Returns `204 No Content` on success and sets the auth cookie.
  - Returns generic `401 ProblemDetails` on invalid credentials, unconfirmed email, or lockout.
- `POST /api/auth/logout`
  - Authorized, CSRF-protected.
  - Returns `204 No Content` and clears the auth cookie.
- `POST /api/auth/confirm-email`
  - Anonymous, CSRF-protected.
  - Body: `ConfirmEmailRequestDto`.
  - Returns `204 No Content` on success.
  - Returns generic `400 ProblemDetails` for invalid/expired token.
- `POST /api/auth/forgot-password`
  - Anonymous, CSRF-protected.
  - Body: `ForgotPasswordRequestDto`.
  - Always returns generic `204 No Content` for syntactically valid email.
- `POST /api/auth/reset-password`
  - Anonymous, CSRF-protected.
  - Body: `ResetPasswordRequestDto`.
  - Returns `204 No Content` on success.
  - Returns `400 ValidationProblemDetails` for invalid password format.
  - Returns generic `400 ProblemDetails` for invalid/expired token.
- `POST /api/auth/resend-confirmation`
  - Anonymous, CSRF-protected.
  - Body: `ResendConfirmationRequestDto`.
  - Always returns generic `204 No Content` for syntactically valid email.

### Favorites Endpoints

- `GET /api/me/lodge-favorites`
  - Authorized.
  - Returns `200 LodgeSummaryDto[]`, sorted by `name ASC`.
- `GET /api/me/lodge-favorites/{lodgeId}`
  - Authorized.
  - Returns `200 LodgeFavoriteStateDto`.
  - Shape: `{ "lodgeId": 5, "isFavorite": true }`.
  - For existing lodge not favorited: `{ "lodgeId": 5, "isFavorite": false }`.
  - For nonexistent lodge: `404 ProblemDetails`.
- `POST /api/me/lodge-favorites/{lodgeId}`
  - Authorized, CSRF-protected.
  - Returns `204 No Content` whether the favorite was newly created or already existed.
  - Returns `404 ProblemDetails` for nonexistent lodge.
- `DELETE /api/me/lodge-favorites/{lodgeId}`
  - Authorized, CSRF-protected.
  - Returns `204 No Content` whether the favorite existed or was already absent.

## Web Routes

- `/login`
  - Email, password, remember-me.
  - Supports `returnUrl` search param.
  - On success redirects to a safe local return URL or `/`.
- `/register`
  - Email and password.
  - On accepted registration redirects to `/check-email?email=<encoded>`.
- `/check-email`
  - Generic “check your email” screen.
  - Links to `/resend-confirmation`.
- `/confirm-email`
  - Reads `userId`, `code`, and optional `returnUrl`.
  - Calls `POST /api/auth/confirm-email`.
  - Shows success and links to `/login`.
- `/forgot-password`
  - Email form.
  - Always shows generic sent-state for valid email format.
- `/reset-password`
  - Reads `email` and `code`.
  - Lets user set new password.
  - On success links to `/login`.
- `/resend-confirmation`
  - Email form.
  - Always shows generic sent-state for valid email format.
- `/favorites`
  - Server-rendered protected page.
  - Redirects unauthenticated users to `/login?returnUrl=/favorites`.
  - Shows current user’s favorite lodges sorted by name.
- `/lodges/[id]`
  - Still public.
  - Server-fetches `/api/auth/me`.
  - If authenticated, server-fetches favorite state and renders favorite client component.
  - If unauthenticated, does not render a favorite control.

## File Structure

### API Files

- Modify: `docker-compose.yml`
  - Add Mailpit service with SMTP and web UI ports.
- Modify: `apps/api/api.csproj`
  - Add Identity EF Core package.
  - Add MailKit package if using MailKit for SMTP.
- Modify: `apps/api/appsettings.json`
  - Add auth/email/client URL settings placeholders.
- Modify: `apps/api/appsettings.Development.json`
  - Add Mailpit SMTP settings and `ClientApp:BaseUrl`.
- Modify: `apps/api/Program.cs`
  - Register Identity, authentication cookies, authorization, antiforgery, email sender, auth/favorite services.
  - Put `UseAuthentication()` before `UseAuthorization()`.
  - Configure cookie and antiforgery options.
- Modify: `apps/api/Data/ApplicationDbContext.cs`
  - Change base class to `IdentityDbContext<ApplicationUser>`.
  - Add `DbSet<LodgeFavorite> LodgeFavorites`.
  - Configure `LodgeFavorite` composite key, FKs, cascade delete, `CreatedAt`, and indexes.
- Create: `apps/api/Models/ApplicationUser.cs`
  - First-party user model.
- Create: `apps/api/Models/LodgeFavorite.cs`
  - Join entity for users and lodges.
- Create: `apps/api/Dtos/AuthDtos.cs`
  - Request/response DTOs for auth endpoints.
- Create: `apps/api/Dtos/LodgeFavoriteDtos.cs`
  - Favorite state DTO.
- Create: `apps/api/Services/IEmailSender.cs`
  - App-owned email abstraction.
- Create: `apps/api/Services/SmtpEmailSender.cs`
  - SMTP implementation using Mailpit settings locally.
- Create: `apps/api/Services/IAuthEmailService.cs`
  - Builds confirmation/reset URLs and sends account emails.
- Create: `apps/api/Services/AuthEmailService.cs`
  - Identity token URL generation plus email body composition.
- Create: `apps/api/Services/ILodgeFavoriteService.cs`
  - Favorite read/create/delete interface.
- Create: `apps/api/Services/LodgeFavoriteService.cs`
  - EF Core implementation.
- Create: `apps/api/Controllers/AuthController.cs`
  - Custom `/api/auth/*` endpoints.
- Create: `apps/api/Controllers/MeLodgeFavoritesController.cs`
  - Custom `/api/me/lodge-favorites*` endpoints.
- Create: `apps/api/Migrations/<timestamp>_AddIdentityAndLodgeFavorites.cs`
  - Created with `dotnet ef migrations add AddIdentityAndLodgeFavorites`.
- Modify: `apps/api/Migrations/AppDbContextModelSnapshot.cs`
  - Generated by EF.
- Create/modify: `apps/api/http/auth.http`
  - Manual auth flow requests.
- Create/modify: `apps/api/http/lodge-favorites.http`
  - Manual favorite endpoint requests.

### Web Files

- Modify: `apps/web/next.config.ts`
  - Add `/api/:path*` rewrite to `API_BASE_URL`.
- Modify: `apps/web/orval.config.ts`
  - Use shared custom fetcher/mutator.
  - Use same-origin base URL for browser-facing calls where appropriate.
- Create: `apps/web/lib/api/apiFetch.ts`
  - Shared Orval fetcher with credentials, CSRF header, JSON/problem handling.
- Create: `apps/web/lib/api/csrf.ts`
  - Ensures CSRF cookie exists before mutating requests.
- Create: `apps/web/lib/api/server.ts`
  - Server-side helpers for forwarding request cookies to the API.
- Modify generated files after running Orval:
  - `apps/web/lib/api/generated/orval/**`
- Modify: `apps/web/components/SiteHeader.tsx`
  - Render auth-aware navigation.
  - Show Login/Register for logged-out users.
  - Show Favorites, email, and Logout for logged-in users.
- Create: `apps/web/components/LogoutButton.tsx`
  - Client component using generated logout endpoint.
- Create: `apps/web/components/FavoriteLodgeButton.tsx`
  - Client component with optimistic save/unsave behavior.
- Create: `apps/web/components/AuthFormField.tsx`
  - Small shared field component if auth form duplication becomes noisy.
- Create: `apps/web/app/login/page.tsx`
- Create: `apps/web/app/register/page.tsx`
- Create: `apps/web/app/check-email/page.tsx`
- Create: `apps/web/app/confirm-email/page.tsx`
- Create: `apps/web/app/forgot-password/page.tsx`
- Create: `apps/web/app/reset-password/page.tsx`
- Create: `apps/web/app/resend-confirmation/page.tsx`
- Create: `apps/web/app/favorites/page.tsx`
- Create: `apps/web/app/favorites/loading.tsx`
- Modify: `apps/web/app/lodges/[id]/page.tsx`
  - Server-fetch current user and favorite state.
  - Render favorite control only when authenticated.

---

### Task 1: Add Mailpit To Local Infrastructure

**Files:**
- Modify: `docker-compose.yml`

- [ ] **Step 1: Update Docker Compose**

Change `docker-compose.yml` to:

```yaml
services:
  postgres:
    image: postgres:18
    container_name: alpinarc-db
    environment:
      POSTGRES_USER: alpinarcuser
      POSTGRES_PASSWORD: alpinarc
      POSTGRES_DB: alpinarc
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql

  mailpit:
    image: axllent/mailpit:latest
    container_name: alpinarc-mailpit
    ports:
      - "1025:1025"
      - "8025:8025"

volumes:
  pgdata:
```

- [ ] **Step 2: Start local services**

Run:

```bash
docker compose up -d
```

Expected:

```text
Container alpinarc-db       Running
Container alpinarc-mailpit  Running
```

- [ ] **Step 3: Verify Mailpit is reachable**

Run:

```bash
curl -I http://localhost:8025
```

Expected: HTTP `200` or `405` response from Mailpit, not connection refused.

- [ ] **Step 4: Commit**

```bash
git add docker-compose.yml
git commit -m "chore: add local mailpit service"
```

---

### Task 2: Add Identity And Email Configuration

**Files:**
- Modify: `apps/api/api.csproj`
- Modify: `apps/api/appsettings.json`
- Modify: `apps/api/appsettings.Development.json`

- [ ] **Step 1: Add NuGet packages**

Run:

```bash
dotnet add apps/api/api.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.9
dotnet add apps/api/api.csproj package MailKit --version 4.14.1
```

Expected: both package references are added to `apps/api/api.csproj`.

- [ ] **Step 2: Add base configuration shape**

Add these sections to `apps/api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Postgres": ""
  },
  "ClientApp": {
    "BaseUrl": ""
  },
  "Email": {
    "FromAddress": "no-reply@alpinarc.local",
    "FromName": "AlpinArc",
    "Smtp": {
      "Host": "",
      "Port": 25,
      "UseSsl": false,
      "Username": "",
      "Password": ""
    }
  }
}
```

If `appsettings.json` already has `ConnectionStrings`, merge the `ClientApp` and `Email` sections without removing existing logging or host settings.

- [ ] **Step 3: Add development configuration**

Add these sections to `apps/api/appsettings.Development.json` while preserving the existing Postgres connection string:

```json
{
  "ClientApp": {
    "BaseUrl": "http://localhost:3000"
  },
  "Email": {
    "FromAddress": "no-reply@alpinarc.local",
    "FromName": "AlpinArc",
    "Smtp": {
      "Host": "localhost",
      "Port": 1025,
      "UseSsl": false,
      "Username": "",
      "Password": ""
    }
  }
}
```

- [ ] **Step 4: Build the API**

Run:

```bash
dotnet build apps/api/api.csproj
```

Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add apps/api/api.csproj apps/api/appsettings.json apps/api/appsettings.Development.json
git commit -m "chore: configure identity email dependencies"
```

---

### Task 3: Add Identity User And Favorite Data Model

**Files:**
- Create: `apps/api/Models/ApplicationUser.cs`
- Create: `apps/api/Models/LodgeFavorite.cs`
- Modify: `apps/api/Data/ApplicationDbContext.cs`

- [ ] **Step 1: Create the Identity user model**

Create `apps/api/Models/ApplicationUser.cs`:

```csharp
using Microsoft.AspNetCore.Identity;

namespace Api.Models;

public sealed class ApplicationUser : IdentityUser
{
    public ICollection<LodgeFavorite> LodgeFavorites { get; } = [];
}
```

- [ ] **Step 2: Create the favorite join model**

Create `apps/api/Models/LodgeFavorite.cs`:

```csharp
namespace Api.Models;

public sealed class LodgeFavorite
{
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public long LodgeId { get; set; }

    public Lodge Lodge { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
}
```

- [ ] **Step 3: Convert the DbContext to IdentityDbContext**

In `apps/api/Data/ApplicationDbContext.cs`, add:

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
```

Change the class declaration from:

```csharp
public class AppDbContext : DbContext
```

to:

```csharp
public class AppDbContext : IdentityDbContext<ApplicationUser>
```

Add the favorite set:

```csharp
public DbSet<LodgeFavorite> LodgeFavorites => Set<LodgeFavorite>();
```

At the top of `OnModelCreating`, call the base model builder before custom entities:

```csharp
base.OnModelCreating(modelBuilder);
modelBuilder.HasPostgresExtension("citext");
```

Add this entity configuration inside `OnModelCreating`:

```csharp
modelBuilder.Entity<LodgeFavorite>(entity =>
{
    entity.HasKey(e => new { e.UserId, e.LodgeId });
    entity.Property(e => e.UserId).IsRequired();
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

    entity.HasIndex(e => e.LodgeId);

    entity.HasOne(e => e.User)
        .WithMany(e => e.LodgeFavorites)
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(e => e.Lodge)
        .WithMany()
        .HasForeignKey(e => e.LodgeId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

- [ ] **Step 4: Build the API**

Run:

```bash
dotnet build apps/api/api.csproj
```

Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add apps/api/Models/ApplicationUser.cs apps/api/Models/LodgeFavorite.cs apps/api/Data/ApplicationDbContext.cs
git commit -m "feat: add identity user and lodge favorite model"
```

---

### Task 4: Wire Identity, Cookies, Lockout, And Antiforgery

**Files:**
- Modify: `apps/api/Program.cs`

- [ ] **Step 1: Add required using statements**

Add to `apps/api/Program.cs`:

```csharp
using Api.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
```

- [ ] **Step 2: Register Identity**

After `builder.Services.AddControllers();`, add:

```csharp
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedEmail = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
```

- [ ] **Step 3: Configure application cookie**

After Identity registration, add:

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "__Host-AlpinArc.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
```

- [ ] **Step 4: Register antiforgery**

Add:

```csharp
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "__Host-AlpinArc.Xsrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.HeaderName = "X-XSRF-TOKEN";
});
```

The readable token cookie will be set by the auth controller as `XSRF-TOKEN`; the antiforgery system cookie remains HTTP-only.

- [ ] **Step 5: Add authentication middleware**

Change:

```csharp
app.UseHttpsRedirection();
app.UseAuthorization();
```

to:

```csharp
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
```

- [ ] **Step 6: Build the API**

Run:

```bash
dotnet build apps/api/api.csproj
```

Expected: build succeeds.

- [ ] **Step 7: Commit**

```bash
git add apps/api/Program.cs
git commit -m "feat: configure identity cookie authentication"
```

---

### Task 5: Add Auth DTOs And Email Services

**Files:**
- Create: `apps/api/Dtos/AuthDtos.cs`
- Create: `apps/api/Services/IEmailSender.cs`
- Create: `apps/api/Services/SmtpEmailSender.cs`
- Create: `apps/api/Services/IAuthEmailService.cs`
- Create: `apps/api/Services/AuthEmailService.cs`
- Modify: `apps/api/Program.cs`

- [ ] **Step 1: Create auth DTOs**

Create `apps/api/Dtos/AuthDtos.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace Api.Dtos;

public sealed record CurrentUserDto(bool IsAuthenticated, string? Email);

public sealed record RegisterRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    public bool RememberMe { get; init; }
}

public sealed record ConfirmEmailRequestDto
{
    [Required]
    public string UserId { get; init; } = string.Empty;

    [Required]
    public string Code { get; init; } = string.Empty;
}

public sealed record ForgotPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
}

public sealed record ResetPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Code { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record ResendConfirmationRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
}
```

- [ ] **Step 2: Create generic email sender interface**

Create `apps/api/Services/IEmailSender.cs`:

```csharp
namespace Api.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Create SMTP email sender**

Create `apps/api/Services/SmtpEmailSender.cs`:

```csharp
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Api.Services;

public sealed class EmailOptions
{
    public string FromAddress { get; init; } = "no-reply@alpinarc.local";
    public string FromName { get; init; } = "AlpinArc";
    public SmtpOptions Smtp { get; init; } = new();
}

public sealed class SmtpOptions
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 25;
    public bool UseSsl { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = _options.Smtp.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
        await client.ConnectAsync(_options.Smtp.Host, _options.Smtp.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.Smtp.Username))
        {
            await client.AuthenticateAsync(_options.Smtp.Username, _options.Smtp.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
```

- [ ] **Step 4: Create auth email service interface**

Create `apps/api/Services/IAuthEmailService.cs`:

```csharp
using Api.Models;

namespace Api.Services;

public interface IAuthEmailService
{
    Task SendConfirmationEmailAsync(ApplicationUser user, CancellationToken cancellationToken);

    Task SendPasswordResetEmailAsync(ApplicationUser user, CancellationToken cancellationToken);
}
```

- [ ] **Step 5: Create auth email service**

Create `apps/api/Services/AuthEmailService.cs`:

```csharp
using System.Text.Encodings.Web;
using Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Api.Services;

public sealed class AuthEmailService(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    IConfiguration configuration) : IAuthEmailService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IEmailSender _emailSender = emailSender;
    private readonly IConfiguration _configuration = configuration;

    public async Task SendConfirmationEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var url = BuildClientUrl("/confirm-email", new Dictionary<string, string?>
        {
            ["userId"] = user.Id,
            ["code"] = code,
            ["returnUrl"] = "/login"
        });

        var encodedUrl = HtmlEncoder.Default.Encode(url);
        await _emailSender.SendEmailAsync(
            user.Email,
            "Confirm your AlpinArc account",
            $"""
            <p>Confirm your AlpinArc account by opening this link:</p>
            <p><a href="{encodedUrl}">Confirm email</a></p>
            """,
            cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        var url = BuildClientUrl("/reset-password", new Dictionary<string, string?>
        {
            ["email"] = user.Email,
            ["code"] = code
        });

        var encodedUrl = HtmlEncoder.Default.Encode(url);
        await _emailSender.SendEmailAsync(
            user.Email,
            "Reset your AlpinArc password",
            $"""
            <p>Reset your AlpinArc password by opening this link:</p>
            <p><a href="{encodedUrl}">Reset password</a></p>
            """,
            cancellationToken);
    }

    private string BuildClientUrl(string path, IDictionary<string, string?> query)
    {
        var baseUrl = _configuration["ClientApp:BaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("ClientApp:BaseUrl is not configured.");
        }

        return QueryHelpers.AddQueryString($"{baseUrl}{path}", query);
    }
}
```

- [ ] **Step 6: Register services**

In `apps/api/Program.cs`, add:

```csharp
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IAuthEmailService, AuthEmailService>();
```

- [ ] **Step 7: Build the API**

Run:

```bash
dotnet build apps/api/api.csproj
```

Expected: build succeeds.

- [ ] **Step 8: Commit**

```bash
git add apps/api/Dtos/AuthDtos.cs apps/api/Services/IEmailSender.cs apps/api/Services/SmtpEmailSender.cs apps/api/Services/IAuthEmailService.cs apps/api/Services/AuthEmailService.cs apps/api/Program.cs
git commit -m "feat: add account email services"
```

---

### Task 6: Add Auth Controller With CSRF Endpoint

**Files:**
- Create: `apps/api/Controllers/AuthController.cs`

- [ ] **Step 1: Create the controller**

Create `apps/api/Controllers/AuthController.cs`:

```csharp
using Api.Dtos;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/auth")]
[ApiController]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IAuthEmailService authEmailService,
    IAntiforgery antiforgery) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly IAuthEmailService _authEmailService = authEmailService;
    private readonly IAntiforgery _antiforgery = antiforgery;

    [HttpGet("csrf", Name = "getCsrfToken")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult GetCsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken ?? string.Empty, new CookieOptions
        {
            HttpOnly = false,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps
        });

        return NoContent();
    }

    [HttpGet("me", Name = "getCurrentUser")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public ActionResult<CurrentUserDto> Me()
    {
        var email = User.Identity?.IsAuthenticated == true
            ? User.Identity.Name
            : null;

        return Ok(new CurrentUserDto(email is not null, email));
    }

    [HttpPost("register", Name = "register")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim();
        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);

        if (existingUser is not null)
        {
            if (!await _userManager.IsEmailConfirmedAsync(existingUser))
            {
                await _authEmailService.SendConfirmationEmailAsync(existingUser, cancellationToken);
            }

            return NoContent();
        }

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return ValidationProblem(ModelState);
        }

        await _authEmailService.SendConfirmationEmailAsync(user, cancellationToken);
        return NoContent();
    }

    [HttpPost("login", Name = "login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        var result = await _signInManager.PasswordSignInAsync(
            request.Email.Trim(),
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        return result.Succeeded
            ? NoContent()
            : Problem("Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);
    }

    [HttpPost("logout", Name = "logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return NoContent();
    }

    [HttpPost("confirm-email", Name = "confirmEmail")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequestDto request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return Problem("The confirmation link is invalid or expired.", statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Code);
        return result.Succeeded
            ? NoContent()
            : Problem("The confirmation link is invalid or expired.", statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpPost("forgot-password", Name = "forgotPassword")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is not null && await _userManager.IsEmailConfirmedAsync(user))
        {
            await _authEmailService.SendPasswordResetEmailAsync(user, cancellationToken);
        }

        return NoContent();
    }

    [HttpPost("reset-password", Name = "resetPassword")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return Problem("The reset link is invalid or expired.", statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Code, request.Password);
        if (result.Succeeded)
        {
            return NoContent();
        }

        AddIdentityErrors(result);
        return ValidationProblem(ModelState);
    }

    [HttpPost("resend-confirmation", Name = "resendConfirmation")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is not null && !await _userManager.IsEmailConfirmedAsync(user))
        {
            await _authEmailService.SendConfirmationEmailAsync(user, cancellationToken);
        }

        return NoContent();
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }
    }
}
```

- [ ] **Step 2: Build the API**

Run:

```bash
dotnet build apps/api/api.csproj
```

Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add apps/api/Controllers/AuthController.cs
git commit -m "feat: add custom auth endpoints"
```

---

### Task 7: Add Lodge Favorite Service And Controller

**Files:**
- Create: `apps/api/Dtos/LodgeFavoriteDtos.cs`
- Create: `apps/api/Services/ILodgeFavoriteService.cs`
- Create: `apps/api/Services/LodgeFavoriteService.cs`
- Create: `apps/api/Controllers/MeLodgeFavoritesController.cs`
- Modify: `apps/api/Program.cs`

- [ ] **Step 1: Create favorite DTO**

Create `apps/api/Dtos/LodgeFavoriteDtos.cs`:

```csharp
namespace Api.Dtos;

public sealed record LodgeFavoriteStateDto(long LodgeId, bool IsFavorite);
```

- [ ] **Step 2: Create service interface**

Create `apps/api/Services/ILodgeFavoriteService.cs`:

```csharp
using Api.Dtos;

namespace Api.Services;

public interface ILodgeFavoriteService
{
    Task<IReadOnlyCollection<LodgeSummaryDto>> GetFavoritesAsync(string userId, CancellationToken cancellationToken);

    Task<ServiceResult<LodgeFavoriteStateDto>> GetFavoriteStateAsync(string userId, long lodgeId, CancellationToken cancellationToken);

    Task<ServiceResult> AddFavoriteAsync(string userId, long lodgeId, CancellationToken cancellationToken);

    Task<ServiceResult> RemoveFavoriteAsync(string userId, long lodgeId, CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Create service implementation**

Create `apps/api/Services/LodgeFavoriteService.cs`:

```csharp
using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class LodgeFavoriteService(AppDbContext dbContext) : ILodgeFavoriteService
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<IReadOnlyCollection<LodgeSummaryDto>> GetFavoritesAsync(string userId, CancellationToken cancellationToken)
    {
        return await _dbContext.LodgeFavorites
            .AsNoTracking()
            .Where(favorite => favorite.UserId == userId)
            .OrderBy(favorite => favorite.Lodge.Name)
            .Select(favorite => new LodgeSummaryDto(
                favorite.Lodge.Id,
                favorite.Lodge.Name,
                favorite.Lodge.Description,
                favorite.Lodge.CountryCode))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<LodgeFavoriteStateDto>> GetFavoriteStateAsync(string userId, long lodgeId, CancellationToken cancellationToken)
    {
        var lodgeExists = await _dbContext.Lodges
            .AsNoTracking()
            .AnyAsync(lodge => lodge.Id == lodgeId, cancellationToken);

        if (!lodgeExists)
        {
            return ServiceResult<LodgeFavoriteStateDto>.NotFound("Lodge was not found.");
        }

        var isFavorite = await _dbContext.LodgeFavorites
            .AsNoTracking()
            .AnyAsync(favorite => favorite.UserId == userId && favorite.LodgeId == lodgeId, cancellationToken);

        return ServiceResult<LodgeFavoriteStateDto>.Success(new LodgeFavoriteStateDto(lodgeId, isFavorite));
    }

    public async Task<ServiceResult> AddFavoriteAsync(string userId, long lodgeId, CancellationToken cancellationToken)
    {
        var lodgeExists = await _dbContext.Lodges
            .AsNoTracking()
            .AnyAsync(lodge => lodge.Id == lodgeId, cancellationToken);

        if (!lodgeExists)
        {
            return ServiceResult.NotFound("Lodge was not found.");
        }

        var exists = await _dbContext.LodgeFavorites
            .AnyAsync(favorite => favorite.UserId == userId && favorite.LodgeId == lodgeId, cancellationToken);

        if (!exists)
        {
            _dbContext.LodgeFavorites.Add(new LodgeFavorite
            {
                UserId = userId,
                LodgeId = lodgeId
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RemoveFavoriteAsync(string userId, long lodgeId, CancellationToken cancellationToken)
    {
        var favorite = await _dbContext.LodgeFavorites
            .SingleOrDefaultAsync(favorite => favorite.UserId == userId && favorite.LodgeId == lodgeId, cancellationToken);

        if (favorite is not null)
        {
            _dbContext.LodgeFavorites.Remove(favorite);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult.Success();
    }
}
```

If `LodgeSummaryDto` is not currently positional or does not include `Description` and `CountryCode`, adjust the projection to the existing DTO constructor/properties instead of changing the DTO contract in this task.

- [ ] **Step 4: Create controller**

Create `apps/api/Controllers/MeLodgeFavoritesController.cs`:

```csharp
using System.Security.Claims;
using Api.Dtos;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/me/lodge-favorites")]
[ApiController]
[Authorize]
public sealed class MeLodgeFavoritesController(ILodgeFavoriteService lodgeFavoriteService) : ControllerBase
{
    private readonly ILodgeFavoriteService _lodgeFavoriteService = lodgeFavoriteService;

    [HttpGet(Name = "getMyLodgeFavorites")]
    [ProducesResponseType(typeof(IReadOnlyCollection<LodgeSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<LodgeSummaryDto>>> GetFavorites(CancellationToken cancellationToken)
    {
        var favorites = await _lodgeFavoriteService.GetFavoritesAsync(GetUserId(), cancellationToken);
        return Ok(favorites);
    }

    [HttpGet("{lodgeId}", Name = "getMyLodgeFavoriteState")]
    [ProducesResponseType(typeof(LodgeFavoriteStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LodgeFavoriteStateDto>> GetFavoriteState(long lodgeId, CancellationToken cancellationToken)
    {
        var result = await _lodgeFavoriteService.GetFavoriteStateAsync(GetUserId(), lodgeId, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("{lodgeId}", Name = "addMyLodgeFavorite")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddFavorite(long lodgeId, CancellationToken cancellationToken)
    {
        var result = await _lodgeFavoriteService.AddFavoriteAsync(GetUserId(), lodgeId, cancellationToken);
        return this.ToNoContentActionResult(result);
    }

    [HttpDelete("{lodgeId}", Name = "removeMyLodgeFavorite")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFavorite(long lodgeId, CancellationToken cancellationToken)
    {
        await _lodgeFavoriteService.RemoveFavoriteAsync(GetUserId(), lodgeId, cancellationToken);
        return NoContent();
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user is missing a name identifier claim.");
    }
}
```

- [ ] **Step 5: Register favorite service**

In `apps/api/Program.cs`, add:

```csharp
builder.Services.AddScoped<ILodgeFavoriteService, LodgeFavoriteService>();
```

- [ ] **Step 6: Build the API**

Run:

```bash
dotnet build apps/api/api.csproj
```

Expected: build succeeds.

- [ ] **Step 7: Commit**

```bash
git add apps/api/Dtos/LodgeFavoriteDtos.cs apps/api/Services/ILodgeFavoriteService.cs apps/api/Services/LodgeFavoriteService.cs apps/api/Controllers/MeLodgeFavoritesController.cs apps/api/Program.cs
git commit -m "feat: add lodge favorite endpoints"
```

---

### Task 8: Create And Apply EF Migration

**Files:**
- Create: `apps/api/Migrations/<timestamp>_AddIdentityAndLodgeFavorites.cs`
- Create: `apps/api/Migrations/<timestamp>_AddIdentityAndLodgeFavorites.Designer.cs`
- Modify: `apps/api/Migrations/AppDbContextModelSnapshot.cs`

- [ ] **Step 1: Generate the migration**

Run:

```bash
dotnet ef migrations add AddIdentityAndLodgeFavorites --project apps/api/api.csproj
```

Expected: EF creates a migration containing Identity tables and `lodge_favorites`.

- [ ] **Step 2: Inspect the generated migration**

Verify the migration creates these Identity tables:

```text
asp_net_roles
asp_net_role_claims
asp_net_user_claims
asp_net_user_logins
asp_net_user_roles
asp_net_user_tokens
asp_net_users
```

Verify it creates `lodge_favorites` with:

```text
user_id text not null
lodge_id bigint not null
created_at timestamp with time zone not null default NOW()
primary key (user_id, lodge_id)
foreign key user_id -> asp_net_users(id) on delete cascade
foreign key lodge_id -> lodges(id) on delete cascade
```

- [ ] **Step 3: Apply the migration locally**

Run:

```bash
dotnet ef database update --project apps/api/api.csproj
```

Expected: migration applies against local PostgreSQL.

- [ ] **Step 4: Build the API**

Run:

```bash
dotnet build apps/api/api.csproj
```

Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add apps/api/Migrations
git commit -m "feat: add identity and favorites migration"
```

---

### Task 9: Add Manual API Request Files

**Files:**
- Create: `apps/api/http/auth.http`
- Create: `apps/api/http/lodge-favorites.http`

- [ ] **Step 1: Create auth HTTP requests**

Create `apps/api/http/auth.http`:

```http
@api_HostAddress = http://localhost:5111
@email = user@example.com
@password = Passw0rd!

### Get CSRF token
GET {{api_HostAddress}}/api/auth/csrf

### Register
POST {{api_HostAddress}}/api/auth/register
Content-Type: application/json
X-XSRF-TOKEN: paste-token-from-XSRF-TOKEN-cookie

{
  "email": "{{email}}",
  "password": "{{password}}"
}

### Login
POST {{api_HostAddress}}/api/auth/login
Content-Type: application/json
X-XSRF-TOKEN: paste-token-from-XSRF-TOKEN-cookie

{
  "email": "{{email}}",
  "password": "{{password}}",
  "rememberMe": true
}

### Current user
GET {{api_HostAddress}}/api/auth/me

### Forgot password
POST {{api_HostAddress}}/api/auth/forgot-password
Content-Type: application/json
X-XSRF-TOKEN: paste-token-from-XSRF-TOKEN-cookie

{
  "email": "{{email}}"
}

### Resend confirmation
POST {{api_HostAddress}}/api/auth/resend-confirmation
Content-Type: application/json
X-XSRF-TOKEN: paste-token-from-XSRF-TOKEN-cookie

{
  "email": "{{email}}"
}

### Logout
POST {{api_HostAddress}}/api/auth/logout
X-XSRF-TOKEN: paste-token-from-XSRF-TOKEN-cookie
```

- [ ] **Step 2: Create favorite HTTP requests**

Create `apps/api/http/lodge-favorites.http`:

```http
@api_HostAddress = http://localhost:5111
@lodgeId = 5

### List my favorites
GET {{api_HostAddress}}/api/me/lodge-favorites

### Favorite state
GET {{api_HostAddress}}/api/me/lodge-favorites/{{lodgeId}}

### Add favorite
POST {{api_HostAddress}}/api/me/lodge-favorites/{{lodgeId}}
X-XSRF-TOKEN: paste-token-from-XSRF-TOKEN-cookie

### Remove favorite
DELETE {{api_HostAddress}}/api/me/lodge-favorites/{{lodgeId}}
X-XSRF-TOKEN: paste-token-from-XSRF-TOKEN-cookie
```

- [ ] **Step 3: Commit**

```bash
git add apps/api/http/auth.http apps/api/http/lodge-favorites.http
git commit -m "docs: add auth and favorite api requests"
```

---

### Task 10: Configure Next.js Same-Origin API Rewrite

**Files:**
- Modify: `apps/web/next.config.ts`

- [ ] **Step 1: Update Next config**

Change `apps/web/next.config.ts` to:

```ts
import type { NextConfig } from "next";

const apiBaseUrl = process.env.API_BASE_URL ?? "http://localhost:5111";

const nextConfig: NextConfig = {
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${apiBaseUrl}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
```

- [ ] **Step 2: Run web type check**

Run from `apps/web`:

```bash
npm run check
```

Expected: TypeScript passes.

- [ ] **Step 3: Commit**

```bash
git add apps/web/next.config.ts
git commit -m "feat: proxy api through next origin"
```

---

### Task 11: Add Shared Orval Fetcher And Regenerate Client

**Files:**
- Create: `apps/web/lib/api/apiFetch.ts`
- Create: `apps/web/lib/api/csrf.ts`
- Create: `apps/web/lib/api/server.ts`
- Modify: `apps/web/orval.config.ts`
- Modify generated: `apps/web/lib/api/generated/orval/**`

- [ ] **Step 1: Create CSRF helper**

Create `apps/web/lib/api/csrf.ts`:

```ts
export function readCookie(name: string): string | undefined {
  if (typeof document === "undefined") {
    return undefined;
  }

  return document.cookie
    .split("; ")
    .find((part) => part.startsWith(`${name}=`))
    ?.split("=")
    .slice(1)
    .join("=");
}

export async function ensureCsrfToken(): Promise<string | undefined> {
  let token = readCookie("XSRF-TOKEN");

  if (!token) {
    await fetch("/api/auth/csrf", {
      method: "GET",
      credentials: "same-origin",
    });
    token = readCookie("XSRF-TOKEN");
  }

  return token ? decodeURIComponent(token) : undefined;
}
```

- [ ] **Step 2: Create shared fetcher**

Create `apps/web/lib/api/apiFetch.ts`:

```ts
import { ensureCsrfToken } from "./csrf";

type ApiFetchOptions<TBody> = {
  url: string;
  method: string;
  headers?: HeadersInit;
  params?: Record<string, unknown>;
  data?: TBody;
  signal?: AbortSignal;
};

const mutatingMethods = new Set(["POST", "PUT", "PATCH", "DELETE"]);

export type ApiError = {
  status: number;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
};

export async function apiFetch<TResponse, TBody = unknown>({
  url,
  method,
  headers,
  params,
  data,
  signal,
}: ApiFetchOptions<TBody>): Promise<{ status: number; data: TResponse }> {
  const requestUrl = buildUrl(url, params);
  const requestHeaders = new Headers(headers);

  if (data !== undefined && !requestHeaders.has("Content-Type")) {
    requestHeaders.set("Content-Type", "application/json");
  }

  if (typeof window !== "undefined" && mutatingMethods.has(method.toUpperCase())) {
    const token = await ensureCsrfToken();
    if (token) {
      requestHeaders.set("X-XSRF-TOKEN", token);
    }
  }

  const response = await fetch(requestUrl, {
    method,
    headers: requestHeaders,
    body: data === undefined ? undefined : JSON.stringify(data),
    credentials: "same-origin",
    signal,
  });

  const text = await response.text();
  const parsed = text ? JSON.parse(text) : undefined;

  if (!response.ok) {
    throw {
      status: response.status,
      ...(typeof parsed === "object" && parsed !== null ? parsed : {}),
    } satisfies ApiError;
  }

  return {
    status: response.status,
    data: parsed as TResponse,
  };
}

function buildUrl(url: string, params?: Record<string, unknown>): string {
  const requestUrl = new URL(url, typeof window === "undefined" ? "http://localhost" : window.location.origin);

  for (const [key, value] of Object.entries(params ?? {})) {
    if (value !== undefined && value !== null) {
      requestUrl.searchParams.set(key, String(value));
    }
  }

  return `${requestUrl.pathname}${requestUrl.search}`;
}
```

- [ ] **Step 3: Create server-side cookie forwarding helper**

Create `apps/web/lib/api/server.ts`:

```ts
import { cookies } from "next/headers";

export async function getServerCookieHeader(): Promise<string> {
  const cookieStore = await cookies();
  return cookieStore
    .getAll()
    .map((cookie) => `${cookie.name}=${cookie.value}`)
    .join("; ");
}
```

- [ ] **Step 4: Update Orval config**

Modify `apps/web/orval.config.ts` output override to use the shared mutator:

```ts
override: {
  useTypeOverInterfaces: true,
  mutator: {
    path: "./lib/api/apiFetch.ts",
    name: "apiFetch",
  },
},
```

Keep `client: "fetch"` and `mode: "tags-split"`. Remove the old `baseUrl.runtime` block if generated calls are now same-origin relative URLs. If Orval requires a base URL, use `baseUrl: "/api"` only after confirming the generated function URL shape.

- [ ] **Step 5: Regenerate API client**

Start the API first:

```bash
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5111 dotnet run --project apps/api/api.csproj --no-launch-profile
```

In another shell, run from `apps/web`:

```bash
API_BASE_URL=http://localhost:5111 npm run generate:api
```

Expected: Orval regenerates auth and favorite clients.

- [ ] **Step 6: Type check web**

Run from `apps/web`:

```bash
npm run check
```

Expected: TypeScript passes. If Orval’s mutator signature differs from the `apiFetch` signature above, adjust `apiFetch` to match the generated call shape rather than editing generated files manually.

- [ ] **Step 7: Commit**

```bash
git add apps/web/lib/api/apiFetch.ts apps/web/lib/api/csrf.ts apps/web/lib/api/server.ts apps/web/orval.config.ts apps/web/lib/api/generated/orval
git commit -m "feat: use shared generated api fetcher"
```

---

### Task 12: Add Auth-Aware Header And Logout

**Files:**
- Modify: `apps/web/components/SiteHeader.tsx`
- Create: `apps/web/components/LogoutButton.tsx`

- [ ] **Step 1: Create logout button**

Create `apps/web/components/LogoutButton.tsx`:

```tsx
"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { logout } from "@/lib/api/generated/orval/auth/auth";

export function LogoutButton() {
  const router = useRouter();
  const [pending, setPending] = useState(false);

  async function onClick() {
    setPending(true);
    try {
      await logout();
      router.refresh();
      router.push("/");
    } finally {
      setPending(false);
    }
  }

  return (
    <button type="button" onClick={onClick} disabled={pending}>
      {pending ? "Signing out..." : "Logout"}
    </button>
  );
}
```

- [ ] **Step 2: Make header auth-aware**

Update `apps/web/components/SiteHeader.tsx`:

```tsx
import Link from "next/link";
import { getCurrentUser } from "@/lib/api/generated/orval/auth/auth";
import { getServerCookieHeader } from "@/lib/api/server";
import { LogoutButton } from "./LogoutButton";
import { PageContainer } from "./PageContainer";

export default async function SiteHeader() {
  const cookie = await getServerCookieHeader();
  const currentUserResponse = await getCurrentUser({
    headers: cookie ? { Cookie: cookie } : undefined,
  });
  const currentUser = currentUserResponse.data;

  return (
    <header className="h-16 flex items-center border-b border-gray-200">
      <PageContainer>
        <nav>
          <ul className="flex gap-4 items-center">
            <li>
              <Link href="/">Home</Link>
            </li>
            <li>
              <Link href="/explore">Explore</Link>
            </li>
            <li>
              <Link href="/tours">Tours</Link>
            </li>
            {currentUser.isAuthenticated ? (
              <>
                <li>
                  <Link href="/favorites">Favorites</Link>
                </li>
                <li>{currentUser.email}</li>
                <li>
                  <LogoutButton />
                </li>
              </>
            ) : (
              <>
                <li>
                  <Link href="/login">Login</Link>
                </li>
                <li>
                  <Link href="/register">Register</Link>
                </li>
              </>
            )}
          </ul>
        </nav>
      </PageContainer>
    </header>
  );
}
```

If generated `getCurrentUser` uses a different options shape, inspect the regenerated function and pass headers through the supported request options.

- [ ] **Step 3: Type check web**

Run from `apps/web`:

```bash
npm run check
```

Expected: TypeScript passes.

- [ ] **Step 4: Commit**

```bash
git add apps/web/components/SiteHeader.tsx apps/web/components/LogoutButton.tsx
git commit -m "feat: show authentication state in header"
```

---

### Task 13: Add Login And Registration Pages

**Files:**
- Create: `apps/web/app/login/page.tsx`
- Create: `apps/web/app/register/page.tsx`
- Create: `apps/web/app/check-email/page.tsx`

- [ ] **Step 1: Create login page**

Create `apps/web/app/login/page.tsx`:

```tsx
"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { FormEvent, useState } from "react";
import { login } from "@/lib/api/generated/orval/auth/auth";

function getSafeReturnUrl(value: string | null): string {
  return value?.startsWith("/") && !value.startsWith("//") ? value : "/";
}

export default function LoginPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setPending(true);

    const formData = new FormData(event.currentTarget);

    try {
      await login({
        email: String(formData.get("email") ?? ""),
        password: String(formData.get("password") ?? ""),
        rememberMe: formData.get("rememberMe") === "on",
      });
      router.push(getSafeReturnUrl(searchParams.get("returnUrl")));
      router.refresh();
    } catch {
      setError("Invalid email or password.");
    } finally {
      setPending(false);
    }
  }

  return (
    <main className="mx-auto w-full max-w-md px-4 py-10">
      <h1>Login</h1>
      <form onSubmit={onSubmit} className="mt-6 grid gap-4">
        <label className="grid gap-1">
          <span>Email</span>
          <input name="email" type="email" required autoComplete="email" />
        </label>
        <label className="grid gap-1">
          <span>Password</span>
          <input name="password" type="password" required autoComplete="current-password" />
        </label>
        <label className="flex items-center gap-2">
          <input name="rememberMe" type="checkbox" />
          <span>Remember me</span>
        </label>
        {error ? <p role="alert">{error}</p> : null}
        <button type="submit" disabled={pending}>
          {pending ? "Signing in..." : "Login"}
        </button>
      </form>
    </main>
  );
}
```

- [ ] **Step 2: Create register page**

Create `apps/web/app/register/page.tsx`:

```tsx
"use client";

import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { register } from "@/lib/api/generated/orval/auth/auth";

export default function RegisterPage() {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setPending(true);

    const formData = new FormData(event.currentTarget);
    const email = String(formData.get("email") ?? "");

    try {
      await register({
        email,
        password: String(formData.get("password") ?? ""),
      });
      router.push(`/check-email?email=${encodeURIComponent(email)}`);
    } catch {
      setError("Check the email and password requirements and try again.");
    } finally {
      setPending(false);
    }
  }

  return (
    <main className="mx-auto w-full max-w-md px-4 py-10">
      <h1>Register</h1>
      <form onSubmit={onSubmit} className="mt-6 grid gap-4">
        <label className="grid gap-1">
          <span>Email</span>
          <input name="email" type="email" required autoComplete="email" />
        </label>
        <label className="grid gap-1">
          <span>Password</span>
          <input name="password" type="password" required autoComplete="new-password" />
        </label>
        {error ? <p role="alert">{error}</p> : null}
        <button type="submit" disabled={pending}>
          {pending ? "Creating account..." : "Create account"}
        </button>
      </form>
    </main>
  );
}
```

- [ ] **Step 3: Create check-email page**

Create `apps/web/app/check-email/page.tsx`:

```tsx
import Link from "next/link";

type CheckEmailPageProps = {
  searchParams: Promise<{ email?: string }>;
};

export default async function CheckEmailPage({ searchParams }: CheckEmailPageProps) {
  const { email } = await searchParams;

  return (
    <main className="mx-auto w-full max-w-md px-4 py-10">
      <h1>Check your email</h1>
      <p>
        If this email can be used, we sent a confirmation link
        {email ? ` to ${email}` : ""}.
      </p>
      <Link href="/resend-confirmation">Resend confirmation email</Link>
    </main>
  );
}
```

- [ ] **Step 4: Type check web**

Run from `apps/web`:

```bash
npm run check
```

Expected: TypeScript passes.

- [ ] **Step 5: Commit**

```bash
git add apps/web/app/login/page.tsx apps/web/app/register/page.tsx apps/web/app/check-email/page.tsx
git commit -m "feat: add login and registration pages"
```

---

### Task 14: Add Confirmation And Password Recovery Pages

**Files:**
- Create: `apps/web/app/confirm-email/page.tsx`
- Create: `apps/web/app/forgot-password/page.tsx`
- Create: `apps/web/app/reset-password/page.tsx`
- Create: `apps/web/app/resend-confirmation/page.tsx`

- [ ] **Step 1: Create confirm-email page**

Create `apps/web/app/confirm-email/page.tsx`:

```tsx
"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import { confirmEmail } from "@/lib/api/generated/orval/auth/auth";

export default function ConfirmEmailPage() {
  const searchParams = useSearchParams();
  const [state, setState] = useState<"pending" | "success" | "error">("pending");

  useEffect(() => {
    const userId = searchParams.get("userId");
    const code = searchParams.get("code");

    if (!userId || !code) {
      setState("error");
      return;
    }

    confirmEmail({ userId, code })
      .then(() => setState("success"))
      .catch(() => setState("error"));
  }, [searchParams]);

  return (
    <main className="mx-auto w-full max-w-md px-4 py-10">
      <h1>Confirm email</h1>
      {state === "pending" ? <p>Confirming your email...</p> : null}
      {state === "success" ? (
        <>
          <p>Your email is confirmed.</p>
          <Link href="/login">Login</Link>
        </>
      ) : null}
      {state === "error" ? (
        <>
          <p>The confirmation link is invalid or expired.</p>
          <Link href="/resend-confirmation">Resend confirmation email</Link>
        </>
      ) : null}
    </main>
  );
}
```

- [ ] **Step 2: Create forgot-password page**

Create `apps/web/app/forgot-password/page.tsx`:

```tsx
"use client";

import { FormEvent, useState } from "react";
import { forgotPassword } from "@/lib/api/generated/orval/auth/auth";

export default function ForgotPasswordPage() {
  const [sent, setSent] = useState(false);
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    const formData = new FormData(event.currentTarget);

    try {
      await forgotPassword({ email: String(formData.get("email") ?? "") });
      setSent(true);
    } finally {
      setPending(false);
    }
  }

  return (
    <main className="mx-auto w-full max-w-md px-4 py-10">
      <h1>Forgot password</h1>
      {sent ? (
        <p>If an account exists, we sent reset instructions.</p>
      ) : (
        <form onSubmit={onSubmit} className="mt-6 grid gap-4">
          <label className="grid gap-1">
            <span>Email</span>
            <input name="email" type="email" required autoComplete="email" />
          </label>
          <button type="submit" disabled={pending}>
            {pending ? "Sending..." : "Send reset instructions"}
          </button>
        </form>
      )}
    </main>
  );
}
```

- [ ] **Step 3: Create reset-password page**

Create `apps/web/app/reset-password/page.tsx`:

```tsx
"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { FormEvent, useState } from "react";
import { resetPassword } from "@/lib/api/generated/orval/auth/auth";

export default function ResetPasswordPage() {
  const searchParams = useSearchParams();
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setPending(true);

    const email = searchParams.get("email");
    const code = searchParams.get("code");
    const formData = new FormData(event.currentTarget);

    if (!email || !code) {
      setError("The reset link is invalid or expired.");
      setPending(false);
      return;
    }

    try {
      await resetPassword({
        email,
        code,
        password: String(formData.get("password") ?? ""),
      });
      setSuccess(true);
    } catch {
      setError("The reset link is invalid or expired, or the password does not meet the requirements.");
    } finally {
      setPending(false);
    }
  }

  return (
    <main className="mx-auto w-full max-w-md px-4 py-10">
      <h1>Reset password</h1>
      {success ? (
        <>
          <p>Your password has been reset.</p>
          <Link href="/login">Login</Link>
        </>
      ) : (
        <form onSubmit={onSubmit} className="mt-6 grid gap-4">
          <label className="grid gap-1">
            <span>New password</span>
            <input name="password" type="password" required autoComplete="new-password" />
          </label>
          {error ? <p role="alert">{error}</p> : null}
          <button type="submit" disabled={pending}>
            {pending ? "Resetting..." : "Reset password"}
          </button>
        </form>
      )}
    </main>
  );
}
```

- [ ] **Step 4: Create resend-confirmation page**

Create `apps/web/app/resend-confirmation/page.tsx`:

```tsx
"use client";

import { FormEvent, useState } from "react";
import { resendConfirmation } from "@/lib/api/generated/orval/auth/auth";

export default function ResendConfirmationPage() {
  const [sent, setSent] = useState(false);
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    const formData = new FormData(event.currentTarget);

    try {
      await resendConfirmation({ email: String(formData.get("email") ?? "") });
      setSent(true);
    } finally {
      setPending(false);
    }
  }

  return (
    <main className="mx-auto w-full max-w-md px-4 py-10">
      <h1>Resend confirmation</h1>
      {sent ? (
        <p>If this email can be used, we sent a confirmation link.</p>
      ) : (
        <form onSubmit={onSubmit} className="mt-6 grid gap-4">
          <label className="grid gap-1">
            <span>Email</span>
            <input name="email" type="email" required autoComplete="email" />
          </label>
          <button type="submit" disabled={pending}>
            {pending ? "Sending..." : "Send confirmation link"}
          </button>
        </form>
      )}
    </main>
  );
}
```

- [ ] **Step 5: Type check web**

Run from `apps/web`:

```bash
npm run check
```

Expected: TypeScript passes.

- [ ] **Step 6: Commit**

```bash
git add apps/web/app/confirm-email/page.tsx apps/web/app/forgot-password/page.tsx apps/web/app/reset-password/page.tsx apps/web/app/resend-confirmation/page.tsx
git commit -m "feat: add email confirmation and password recovery pages"
```

---

### Task 15: Add Favorite Toggle To Lodge Detail Page

**Files:**
- Create: `apps/web/components/FavoriteLodgeButton.tsx`
- Modify: `apps/web/app/lodges/[id]/page.tsx`

- [ ] **Step 1: Create favorite button client component**

Create `apps/web/components/FavoriteLodgeButton.tsx`:

```tsx
"use client";

import { useState } from "react";
import {
  addMyLodgeFavorite,
  removeMyLodgeFavorite,
} from "@/lib/api/generated/orval/me-lodge-favorites/me-lodge-favorites";

type FavoriteLodgeButtonProps = {
  lodgeId: number;
  initialIsFavorite: boolean;
};

export function FavoriteLodgeButton({
  lodgeId,
  initialIsFavorite,
}: FavoriteLodgeButtonProps) {
  const [isFavorite, setIsFavorite] = useState(initialIsFavorite);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onClick() {
    const nextValue = !isFavorite;
    setIsFavorite(nextValue);
    setPending(true);
    setError(null);

    try {
      if (nextValue) {
        await addMyLodgeFavorite(lodgeId);
      } else {
        await removeMyLodgeFavorite(lodgeId);
      }
    } catch {
      setIsFavorite(!nextValue);
      setError("Could not update favorite. Try again.");
    } finally {
      setPending(false);
    }
  }

  return (
    <div>
      <button type="button" onClick={onClick} disabled={pending} aria-pressed={isFavorite}>
        {isFavorite ? "Saved" : "Save"}
      </button>
      {error ? <p role="alert">{error}</p> : null}
    </div>
  );
}
```

- [ ] **Step 2: Update lodge detail page**

Update `apps/web/app/lodges/[id]/page.tsx` to fetch auth and favorite state server-side:

```tsx
import { notFound } from "next/navigation";

import { FavoriteLodgeButton } from "@/components/FavoriteLodgeButton";
import { PageContainer } from "@/components/PageContainer";
import { StageList } from "@/components/StageList";
import { TourVariantList } from "@/components/TourVariantList";
import { getCurrentUser } from "@/lib/api/generated/orval/auth/auth";
import { getLodgeById } from "@/lib/api/generated/orval/lodges/lodges";
import { getMyLodgeFavoriteState } from "@/lib/api/generated/orval/me-lodge-favorites/me-lodge-favorites";
import { getTourVariants } from "@/lib/api/generated/orval/tour-variants/tour-variants";
import { getServerCookieHeader } from "@/lib/api/server";

type LodgePageProps = {
  params: Promise<{
    id: string;
  }>;
};

export default async function Lodge({ params }: LodgePageProps) {
  const { id } = await params;
  const cookie = await getServerCookieHeader();
  const [lodgeResponse, tourVariantsResponse, currentUserResponse] =
    await Promise.all([
      getLodgeById(id),
      getTourVariants({ lodgeId: id }),
      getCurrentUser({ headers: cookie ? { Cookie: cookie } : undefined }),
    ]);

  if (lodgeResponse.status === 404 || tourVariantsResponse.status === 404) {
    notFound();
  }

  if (tourVariantsResponse.status !== 200) {
    throw new Error(
      `Failed to load tour variants: ${tourVariantsResponse.status}`,
    );
  }

  const lodge = lodgeResponse.data;
  const { stages } = lodge;
  const currentUser = currentUserResponse.data;

  const favoriteState = currentUser.isAuthenticated
    ? await getMyLodgeFavoriteState(id, {
        headers: cookie ? { Cookie: cookie } : undefined,
      })
    : null;

  return (
    <PageContainer>
      <h1>{lodge.name}</h1>
      <p>{lodge.description}</p>
      {currentUser.isAuthenticated && favoriteState?.status === 200 ? (
        <FavoriteLodgeButton
          lodgeId={Number(lodge.id)}
          initialIsFavorite={favoriteState.data.isFavorite}
        />
      ) : null}
      <ul>
        <li>Id: {lodge.id}</li>
        <li>Country: {lodge.countryCode}</li>
        <li>Created at: {lodge.createdAt}</li>
      </ul>

      <h2>Stages</h2>
      <StageList stages={stages} />

      <h2>Tours</h2>
      <TourVariantList variants={tourVariantsResponse.data} />
    </PageContainer>
  );
}
```

If generated function argument order differs, adapt this call to match generated Orval signatures.

- [ ] **Step 3: Type check web**

Run from `apps/web`:

```bash
npm run check
```

Expected: TypeScript passes.

- [ ] **Step 4: Commit**

```bash
git add apps/web/components/FavoriteLodgeButton.tsx apps/web/app/lodges/[id]/page.tsx
git commit -m "feat: add lodge favorite toggle"
```

---

### Task 16: Add Protected Favorites Page

**Files:**
- Create: `apps/web/app/favorites/page.tsx`
- Create: `apps/web/app/favorites/loading.tsx`

- [ ] **Step 1: Create favorites page**

Create `apps/web/app/favorites/page.tsx`:

```tsx
import { redirect } from "next/navigation";
import { LodgeList } from "@/components/LodgeList";
import { PageContainer } from "@/components/PageContainer";
import { getCurrentUser } from "@/lib/api/generated/orval/auth/auth";
import { getMyLodgeFavorites } from "@/lib/api/generated/orval/me-lodge-favorites/me-lodge-favorites";
import { getServerCookieHeader } from "@/lib/api/server";

export default async function FavoritesPage() {
  const cookie = await getServerCookieHeader();
  const currentUserResponse = await getCurrentUser({
    headers: cookie ? { Cookie: cookie } : undefined,
  });

  if (!currentUserResponse.data.isAuthenticated) {
    redirect("/login?returnUrl=/favorites");
  }

  const favoritesResponse = await getMyLodgeFavorites({
    headers: cookie ? { Cookie: cookie } : undefined,
  });

  if (favoritesResponse.status !== 200) {
    throw new Error(`Failed to load favorites: ${favoritesResponse.status}`);
  }

  return (
    <PageContainer>
      <h1>Favorites</h1>
      {favoritesResponse.data.length > 0 ? (
        <LodgeList lodges={favoritesResponse.data} />
      ) : (
        <p>You have not saved any lodges yet.</p>
      )}
    </PageContainer>
  );
}
```

- [ ] **Step 2: Create loading UI**

Create `apps/web/app/favorites/loading.tsx`:

```tsx
import { LoadingPage } from "@/components/LoadingPage";

export default function FavoritesLoading() {
  return <LoadingPage />;
}
```

- [ ] **Step 3: Type check web**

Run from `apps/web`:

```bash
npm run check
```

Expected: TypeScript passes.

- [ ] **Step 4: Commit**

```bash
git add apps/web/app/favorites/page.tsx apps/web/app/favorites/loading.tsx
git commit -m "feat: add protected favorites page"
```

---

### Task 17: Verify Full Local Auth Flow

**Files:**
- No planned source edits unless verification exposes defects.

- [ ] **Step 1: Start dependencies**

Run:

```bash
docker compose up -d
```

Expected: PostgreSQL and Mailpit are running.

- [ ] **Step 2: Start API**

Run:

```bash
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5111 dotnet run --project apps/api/api.csproj --no-launch-profile
```

Expected: API listens on `http://localhost:5111`.

- [ ] **Step 3: Start web**

Run from `apps/web`:

```bash
API_BASE_URL=http://localhost:5111 npm run dev
```

Expected: Next.js listens on `http://localhost:3000`.

- [ ] **Step 4: Verify same-origin proxy**

Run:

```bash
curl -i http://localhost:3000/api/auth/me
```

Expected:

```text
HTTP/1.1 200 OK
```

Response body contains:

```json
{"isAuthenticated":false,"email":null}
```

- [ ] **Step 5: Verify registration and email confirmation**

In the browser:

1. Open `http://localhost:3000/register`.
2. Register with `user@example.com` and `Passw0rd!`.
3. Open Mailpit at `http://localhost:8025`.
4. Open the confirmation email.
5. Click the confirmation link.
6. Verify `/confirm-email` shows success.

- [ ] **Step 6: Verify login and header state**

In the browser:

1. Open `http://localhost:3000/login`.
2. Login with `user@example.com` and `Passw0rd!`.
3. Verify the header shows `Favorites`, the email address, and `Logout`.

- [ ] **Step 7: Verify hidden favorite button when logged out**

In the browser:

1. Click `Logout`.
2. Open `http://localhost:3000/lodges/5`.
3. Verify no favorite button is visible.

- [ ] **Step 8: Verify favorite toggle when logged in**

In the browser:

1. Login again.
2. Open `http://localhost:3000/lodges/5`.
3. Verify the favorite button is visible.
4. Click `Save`.
5. Verify the button changes to `Saved`.
6. Click `Saved`.
7. Verify the button changes to `Save`.

- [ ] **Step 9: Verify `/favorites` redirect**

In the browser:

1. Logout.
2. Open `http://localhost:3000/favorites`.
3. Verify the app redirects to `/login?returnUrl=/favorites`.
4. Login.
5. Verify the app lands on `/favorites`.

- [ ] **Step 10: Commit any verification fixes**

If verification required source fixes:

```bash
git add <fixed-files>
git commit -m "fix: complete auth and favorites flow"
```

If no fixes were needed, do not create an empty commit.

---

### Task 18: Final Build And Regression Verification

**Files:**
- No planned source edits unless verification exposes defects.

- [ ] **Step 1: Build API**

Run:

```bash
dotnet build apps/api/api.csproj
```

Expected: build succeeds.

- [ ] **Step 2: Regenerate API client from current API**

With API running on `http://localhost:5111`, run from `apps/web`:

```bash
API_BASE_URL=http://localhost:5111 npm run generate:api
```

Expected: generated client is up to date.

- [ ] **Step 3: Type check web**

Run from `apps/web`:

```bash
npm run check
```

Expected: TypeScript passes.

- [ ] **Step 4: Build web**

Run from `apps/web`:

```bash
API_BASE_URL=http://localhost:5111 npm run build
```

Expected: Next.js build succeeds.

- [ ] **Step 5: Inspect git diff**

Run:

```bash
git diff --stat
git diff -- apps/api apps/web docker-compose.yml
```

Expected: changes are scoped to auth, favorites, Mailpit, generated API client, and required migrations.

- [ ] **Step 6: Final commit**

If previous task commits were not made individually, commit the complete feature:

```bash
git add docker-compose.yml apps/api apps/web
git commit -m "feat: add authentication and lodge favorites"
```

If task-level commits already exist, skip this step.

---

## Risks And Implementation Notes

- **CSRF integration must be tested in browser.** API-only curl tests will not fully prove that the readable `XSRF-TOKEN` cookie and generated fetcher header work together.
- **Orval mutator signatures may differ from the draft helper.** Inspect generated output after the first generation and adjust only the shared fetcher/config, not generated files.
- **Cookie forwarding from Server Components depends on generated client options.** If Orval functions do not accept `headers` in the expected shape, add server-specific wrappers in `apps/web/lib/api/server.ts` that use plain `fetch` for `auth/me`, favorites list, and favorite state.
- **Identity table naming will follow EF/Npgsql conventions.** Because the context uses `UseSnakeCaseNamingConvention()`, table and column names should be snake_case. Inspect the generated migration before applying.
- **Email token query strings can contain special characters.** Always let `QueryHelpers.AddQueryString` build URLs; do not concatenate token query parameters manually.
- **Account enumeration protections are product requirements.** Do not “improve” UX by revealing duplicate email, unconfirmed account, or missing reset account states.
- **Favorite button visibility is UX only.** The API must stay protected with `[Authorize]`; hiding the button is not authorization.

## Self-Review

- Spec coverage:
  - ASP.NET Core Identity cookie auth: Tasks 2-6.
  - Required email confirmation: Tasks 5-6, 13-14, 17.
  - Local email tool: Task 1 and Task 5.
  - Public registration and email-only accounts: Tasks 5-6, 13.
  - Privacy-preserving auth responses: Task 6 and Risks section.
  - CSRF: Tasks 4, 6-7, 11.
  - Same-origin local simulation: Task 10 and Task 17.
  - Orval shared fetcher: Task 11.
  - Private lodge favorites: Tasks 3, 7-8, 15-16.
  - Hidden favorite button when logged out: Task 15 and Task 17.
  - `/favorites` redirect with return URL: Task 16 and Task 17.
  - Password reset and resend confirmation: Tasks 6 and 14.
  - Remember me 14 days and lockout: Task 4 and Task 13.
- Placeholder scan:
  - No forbidden placeholder markers remain.
  - Notes that depend on generated Orval signatures are explicitly verification instructions, not missing design.
- Type consistency:
  - DTO names introduced in Task 5 are used by Task 6.
  - `LodgeFavoriteStateDto` introduced in Task 7 is used by Task 15.
  - `ApplicationUser` and `LodgeFavorite` are introduced before `AppDbContext` uses them.
  - `IEmailSender` and `IAuthEmailService` are registered after creation.
