# Feature Plans and Documentation

This file consolidates feature planning and documentation content from the following sources:
- AUTHENTICATION_IMPLEMENTATION_PLAN.md
- CORPORATE_FEATURES_IMPLEMENTATION_PLAN.md
- POWERBI_IMPLEMENTATION_PLAN.md
- THEME_SYSTEM.md

---

## Corporate Features Implementation Plan

This document outlines the implementation plan for high-priority corporate features to make PBIHoster more appealing to enterprise customers.

---

### Executive Summary

Goal: Transform PBIHoster into an enterprise-grade Power BI hosting platform with advanced security, compliance, and user experience features.

Timeline: Phased approach over 3-4 months

Prioritization: Based on Impact vs. Effort analysis

Target Users: Large enterprises, regulated industries, MSPs, and SaaS vendors

---

### Feature Roadmap

#### Phase 1: Quick Wins (2-3 weeks)
- [ ] RLS (Row-Level Security) Management UI
- [x] White-Label Customization
- [x] Favorites and Bookmarks
- [ ] Data Refresh Management

#### Phase 2: SSO and Advanced Authentication (2-3 weeks)
- [ ] Azure AD Groups Sync

#### Phase 3: Analytics and Monitoring (2 weeks)
- [ ] Usage Analytics Dashboard
- [ ] Performance Monitoring

#### Phase 4: Collaboration and Governance (3 weeks)
- [ ] Embedded Comments and Annotations
- [ ] Compliance and Data Governance
- [ ] Report Versioning and Rollback

#### Phase 5: Advanced Features (4+ weeks)
- [ ] Scheduling and Subscriptions
- [ ] Multi-Tenancy (deferred; clients host separate instances)
- [ ] Backup and Disaster Recovery

---

### Implementation Details

#### 1. RLS Management UI
- Visual interface for assigning RLS roles to users and groups per report and component
- Backend: Extend Page and Layout model, add endpoints for RLS config
- Frontend: New admin panel for RLS, integrate with Page edit modal

#### 2. White-Label Customization
- [x] Logo upload, custom app name, footer links, favicon
- [x] Backend: Branding endpoints, file upload support
- [x] Frontend: Branding manager UI, dynamic header and footer

#### 3. Favorites and Bookmarks
- [x] Star and favorite pages, recent pages list, quick access dropdown
- [x] Backend: Extend AppUser, endpoints for favorites and recent
- [x] Frontend: Star icons in navigation, favorites dropdown

#### 4. Data Refresh Management
- Manual and scheduled dataset refresh, refresh history, notifications
- Backend: Dataset refresh model, scheduler service, Power BI API integration
- Frontend: Admin panel for refresh management

##### Scope and Goals
- Enable admins to trigger refreshes for datasets tied to pages and reports
- Provide scheduled refreshes with calendars, time zones, and retry policies
- Capture refresh history, status, duration, and Power BI correlation IDs
- Provide alerting (email and webhook) on failures and SLA breaches
- Respect Power BI service limits and tenant capacity constraints

##### Assumptions
- PBIHoster uses Power BI REST API with app-only auth (already used for embeds)
- Datasets are discoverable via report ID or dataset ID stored in Page model
- LiteDB is the system of record for refresh schedules and history

##### Data Model (LiteDB)
- DatasetRefreshSchedule
  - Id (Guid)
  - Name
  - DatasetId
  - ReportId (optional)
  - PageId (optional)
  - Enabled
  - Cron (validated, stored as string)
  - TimeZone (IANA string)
  - RetryCount (int)
  - RetryBackoffSeconds (int)
  - NotifyOnSuccess (bool)
  - NotifyOnFailure (bool)
  - NotifyTargets (list: email or webhook)
  - CreatedByUserId
  - CreatedAtUtc
  - UpdatedAtUtc
- DatasetRefreshRun
  - Id (Guid)
  - ScheduleId (nullable for manual)
  - DatasetId
  - TriggeredByUserId (nullable for system)
  - RequestedAtUtc
  - StartedAtUtc
  - CompletedAtUtc
  - Status (Queued | InProgress | Succeeded | Failed | Cancelled)
  - FailureReason
  - PowerBiRequestId
  - PowerBiActivityId
  - RetriesAttempted
  - DurationMs

##### Backend Architecture
- DatasetRefreshService
  - Validates schedule, checks concurrent run limits, enqueues refresh
  - Uses Power BI REST API: POST /groups/{groupId}/datasets/{datasetId}/refreshes
  - Retrieves status via GET /groups/{groupId}/datasets/{datasetId}/refreshes
- RefreshSchedulerHostedService
  - Cron evaluation loop with in-memory queue
  - Uses time zone conversion to compute next run
- RefreshNotificationService
  - Sends email and webhook based on result
  - Includes run summary and correlation IDs
- RefreshMetricsMiddleware
  - Records request duration for API endpoints

##### API Endpoints
- POST /api/refreshes/datasets/{datasetId}/run (Admin)
- GET /api/refreshes/datasets/{datasetId}/history (Admin)
- GET /api/refreshes/schedules (Admin)
- POST /api/refreshes/schedules (Admin)
- PUT /api/refreshes/schedules/{id} (Admin)
- DELETE /api/refreshes/schedules/{id} (Admin)
- POST /api/refreshes/schedules/{id}/toggle (Admin)

##### Frontend UX
- Admin panel: Data Refresh
  - Dataset list with last refresh status and duration
  - Manual refresh button with confirmation
  - Schedule editor (cron builder, time zone select, retries)
  - History table with filters, status pills, export CSV
  - Notification settings (email and webhook targets)

##### Security and Compliance
- Admin-only access
- Audit log entries for manual trigger and schedule changes
- Throttle manual refresh to avoid abuse

##### Power BI Constraints
- Respect dataset refresh limits (max per day, concurrency)
- Handle 429 responses with retry-after
- Avoid refresh during ongoing refresh for same dataset

##### Testing
- Unit tests for cron parsing and time zone conversion
- Integration tests for schedule creation and manual refresh endpoints
- Mock Power BI API for refresh status polling

##### Documentation Deliverables
- Admin guide: Data Refresh Management
  - How to create schedules
  - Manual refresh workflow
  - Interpreting history and failures
  - Power BI limits and best practices
- API docs for all endpoints with example requests and responses
- Runbook: troubleshooting failed refreshes and 429 handling

#### 5. Azure AD Groups Sync
- Sync AD groups to internal roles and groups, periodic and manual sync
- Backend: Microsoft Graph API integration, background sync service
- Frontend: Admin UI for mapping and sync control

#### 6. Usage Analytics Dashboard
- Track and report page views, user activity, performance metrics
- Backend: Analytics service, endpoints for stats
- Frontend: Admin dashboard with charts and export

#### 7. Performance Monitoring
- API and report load time tracking, alerting, health checks
- Backend: Middleware for metrics, alert service
- Frontend: Real-time performance dashboard

#### 8. Embedded Comments and Annotations
- Comment threads, mentions, resolve status, export
- Backend: Comment model and repository, notification service
- Frontend: Comment panel, rich text editor

#### 9. Compliance and Data Governance
- Sensitivity labels, access approval, audit export, GDPR tools
- Backend: Extend Page model, access request workflow, compliance endpoints
- Frontend: Sensitivity selector, compliance dashboard

#### 10. Report Versioning and Rollback
- Track layout changes, version history, rollback UI
- Backend: PageVersion model, version endpoints
- Frontend: Version history modal, diff and rollback tools

#### 11. Scheduling and Subscriptions
- Email subscriptions, scheduled exports, cron UI
- Backend: Subscription model and service, Power BI export, email service
- Frontend: Subscription manager, schedule picker

#### 12. Multi-Tenancy
- Deferred: Clients host separate instances

#### 13. Backup and Disaster Recovery
- Automated backups to cloud, restore, health checks
- Backend: Backup service, cloud storage integration
- Frontend: Backup manager UI

---

### Testing and Documentation
- Unit, integration, and E2E tests for each feature
- User and admin guides, API docs, migration notes

---

### Success Metrics
- Feature adoption rates, reduced support tickets, improved performance, customer feedback, business KPIs

---

### Next Steps
1. Review and approve plan
2. Sprint planning for Phase 1
3. Environment setup (SMTP, storage, etc.)
4. Begin with RLS Management UI

---

Last Updated: 2026-02-06

---

## Authentication Implementation Plan

This document outlines the plan to extend the current authentication system (Basic Authentication) to support external identity providers via OAuth2 and OpenID Connect (OIDC), such as Azure AD (Entra ID), Okta, Auth0, and Clerk.

### 1. Architecture Overview

The system will support two primary modes of authentication, configurable by the administrator:

1. Basic Authentication (Current): Local username and password stored in LiteDB with BCrypt hashing and JWT tokens.
2. External Authentication (New): OAuth2 and OIDC based authentication where an external provider handles identity verification.

#### Authentication Flow Decision

Hybrid Approach (Recommended):
- OIDC providers issue authentication cookies via standard OpenID Connect flow
- After successful OIDC auth, backend generates internal JWT token (same format as Basic auth)
- Frontend uses JWT for all API calls (consistent with current implementation)
- Benefits:
  - No frontend changes to API client (still uses Bearer tokens)
  - OIDC session management handled server-side
  - Supports token refresh without re-authenticating
  - Compatible with SPA architecture

#### Key Concepts
- Configuration: The application will read authentication settings from Environment Variables (or appsettings.json) at startup.
- User Mapping: External users will be mapped to internal AppUser records (JIT sync).
  - Auto-provisioning: Users authenticating successfully via an external provider will have a local account created automatically if one doesn't exist.
  - Lightweight Sync: Only store essential data (ExternalUserId, Email, Roles). PasswordHash remains null for external users.
  - Update on Login: Refresh email and display name from claims on each login to maintain consistency.
- Role Mapping:
  - Option 1 (Simple): Assign default Viewer role on first login, admins manually promote users
  - Option 2 (Advanced): Map external claims and groups to internal roles via configuration (for example, AzureAD_Group_Admins to Admin)
- Mixed Mode Support: System can have both local and external users simultaneously (identified by AuthProvider field).

### 2. Database Schema Changes

#### 2.1 AppUser Model
Extend AppUser to support external identity linking.

```csharp
public class AppUser
{
    // ... existing properties ...

    // New properties
    public string? AuthProvider { get; set; } // "Local", "AzureAd", "Okta", etc.
    public string? ExternalUserId { get; set; } // Subject ID from the provider
}
```

#### 2.2 Configuration (Environment Variables)
Authentication is configured via standard ASP.NET Core configuration providers (Environment Variables, appsettings.json).

| Environment Variable | Description | Example |
| --- | --- | --- |
| Auth__Mode | The active authentication mode. | Basic, OIDC, or Mixed |
| Auth__OIDC__Enabled | Enable OIDC authentication. | true or false |
| Auth__OIDC__ProviderName | Display name for the login button. | Azure AD, Okta |
| Auth__OIDC__Authority | The issuer URL. | https://login.microsoftonline.com/{tenant}/v2.0 |
| Auth__OIDC__ClientId | The client ID. | GUID |
| Auth__OIDC__ClientSecret | The client secret. | secret-value |
| Auth__OIDC__ResponseType | OAuth flow response type. | code (default, most secure) |
| Auth__OIDC__Scopes | Scopes to request. | openid profile email |
| Auth__OIDC__CallbackPath | The redirect URI path. | /signin-oidc |
| Auth__OIDC__SignedOutCallbackPath | Post-logout redirect path. | /signout-callback-oidc |
| Auth__OIDC__RoleClaimType | Claim to use for role mapping. | roles, groups, or http://schemas.microsoft.com/ws/2008/06/identity/claims/role |
| Auth__OIDC__DefaultRole | Default role for new external users. | Viewer |
| Auth__OIDC__RoleMappings__* | Map external groups to roles. | Auth__OIDC__RoleMappings__AdminGroup=Admin |

Note: When Auth__Mode is Mixed, both Basic and OIDC are available. When Basic, only local auth works. When OIDC, external auth is primary but local admin fallback can still be enabled.

### 3. Backend Implementation (ReportTree.Server)

#### 3.1 Authentication Configuration (Program.cs)
Modify the startup logic to configure authentication services based on the configuration.

```csharp
// Pseudo-code for Program.cs
var authMode = builder.Configuration["Auth:Mode"] ?? "Basic";
var oidcEnabled = builder.Configuration.GetValue<bool>("Auth:OIDC:Enabled", false);

var authBuilder = builder.Services.AddAuthentication(options => {
    // Always use JWT Bearer as default for API calls
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
});

// Always configure JWT Bearer (used by both Basic and OIDC flows)
authBuilder.AddJwtBearer(options => {
    // ... existing JWT configuration
});

// Additionally configure OIDC if enabled
if (oidcEnabled || authMode == "OIDC" || authMode == "Mixed")
{
    authBuilder.AddCookie("oidc-session") // Separate cookie scheme for OIDC flow
               .AddOpenIdConnect(options => {
                   options.SignInScheme = "oidc-session";

                   // Configure from Environment Variables and appsettings.json
                   builder.Configuration.Bind("Auth:OIDC", options);

                   // Set required values
                   options.ResponseType = OpenIdConnectResponseType.Code;
                   options.GetClaimsFromUserInfoEndpoint = true;
                   options.SaveTokens = true; // Store tokens in cookie for later use

                   options.Events = new OpenIdConnectEvents
                   {
                       OnTokenValidated = async context =>
                       {
                           // Get the user sync service
                           var userRepo = context.HttpContext.RequestServices
                               .GetRequiredService<IUserRepository>();
                           var tokenService = context.HttpContext.RequestServices
                               .GetRequiredService<ITokenService>();

                           var externalUserId = context.Principal?.FindFirst("sub")?.Value 
                               ?? context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                           var email = context.Principal?.FindFirst("email")?.Value 
                               ?? context.Principal?.FindFirst(ClaimTypes.Email)?.Value;
                           var name = context.Principal?.FindFirst("name")?.Value 
                               ?? context.Principal?.FindFirst(ClaimTypes.Name)?.Value 
                               ?? email;

                           if (string.IsNullOrEmpty(externalUserId))
                           {
                               context.Fail("Unable to retrieve user ID from external provider");
                               return;
                           }

                           // Sync user to local DB
                           var localUser = await SyncExternalUser(
                               userRepo, 
                               externalUserId, 
                               email, 
                               name, 
                               context.Principal,
                               builder.Configuration
                           );

                           // Generate internal JWT token and store in cookie
                           var jwt = tokenService.Generate(localUser);

                           // Add JWT to response cookie (for frontend to retrieve)
                           context.Response.Cookies.Append("app-jwt", jwt, new CookieOptions
                           {
                               HttpOnly = false, // Allow JS to read it
                               Secure = true,
                               SameSite = SameSiteMode.Strict,
                               MaxAge = TimeSpan.FromHours(8)
                           });
                       },
                       OnRemoteFailure = context =>
                       {
                           context.Response.Redirect("/login?error=external_auth_failed");
                           context.HandleResponse();
                           return Task.CompletedTask;
                       }
                   };
               });
}

// Helper method to sync external user
static async Task<AppUser> SyncExternalUser(
    IUserRepository userRepo,
    string externalUserId,
    string email,
    string name,
    ClaimsPrincipal principal,
    IConfiguration config)
{
    var authProvider = config["Auth:OIDC:ProviderName"] ?? "OIDC";

    // Try to find existing user by external ID
    var user = await userRepo.GetByExternalIdAsync(externalUserId);

    if (user == null)
    {
        // Create new user
        var defaultRole = config["Auth:OIDC:DefaultRole"] ?? "Viewer";
        var roles = MapExternalRolesToInternal(principal, config);

        if (!roles.Any())
        {
            roles.Add(defaultRole);
        }

        user = new AppUser
        {
            Username = name ?? email,
            Email = email,
            ExternalUserId = externalUserId,
            AuthProvider = authProvider,
            PasswordHash = null, // No password for external users
            Roles = roles,
            CreatedAt = DateTime.UtcNow
        };

        await userRepo.UpsertAsync(user);
    }
    else
    {
        // Update existing user (refresh email and name)
        user.Email = email;
        user.Username = name ?? email;
        user.LastLogin = DateTime.UtcNow;

        // Optionally update roles from external claims
        var mappedRoles = MapExternalRolesToInternal(principal, config);
        if (mappedRoles.Any())
        {
            user.Roles = mappedRoles;
        }

        await userRepo.UpsertAsync(user);
    }

    return user;
}

static List<string> MapExternalRolesToInternal(ClaimsPrincipal principal, IConfiguration config)
{
    var roles = new List<string>();
    var roleClaimType = config["Auth:OIDC:RoleClaimType"] ?? "roles";

    // Get external groups and roles
    var externalRoles = principal.FindAll(roleClaimType).Select(c => c.Value).ToList();

    // Map using configured mappings
    var mappings = config.GetSection("Auth:OIDC:RoleMappings").Get<Dictionary<string, string>>();
    if (mappings != null)
    {
        foreach (var externalRole in externalRoles)
        {
            if (mappings.TryGetValue(externalRole, out var internalRole))
            {
                roles.Add(internalRole);
            }
        }
    }

    return roles;
}
```

#### 3.2 Repository Updates
Add method to IUserRepository and LiteDbUserRepository:

```csharp
// IUserRepository.cs
public interface IUserRepository
{
    // ... existing methods ...
    Task<AppUser?> GetByExternalIdAsync(string externalUserId);
}

// LiteDbUserRepository.cs
public async Task<AppUser?> GetByExternalIdAsync(string externalUserId)
{
    return await Task.FromResult(
        _collection.FindOne(u => u.ExternalUserId == externalUserId)
    );
}
```

#### 3.3 Controller Updates
Add new endpoints to support OIDC flow:

```csharp
// In Program.cs or new AuthController.cs
app.MapGet("/api/auth/external-login", async (HttpContext context) =>
{
    var authMode = app.Configuration["Auth:Mode"];
    if (authMode != "OIDC" && authMode != "Mixed")
    {
        return Results.BadRequest("External authentication not enabled");
    }

    var properties = new AuthenticationProperties
    {
        RedirectUri = "/api/auth/external-callback"
    };

    return Results.Challenge(properties, new[] { OpenIdConnectDefaults.AuthenticationScheme });
});

app.MapGet("/api/auth/external-callback", async (HttpContext context) =>
{
    // User is authenticated via OIDC cookie at this point
    // JWT token was stored in "app-jwt" cookie by OnTokenValidated event

    // Redirect to frontend with success indicator
    return Results.Redirect("/login/success");
});

app.MapPost("/api/auth/logout", async (HttpContext context) =>
{
    var authProvider = context.User.FindFirst("AuthProvider")?.Value;

    if (authProvider == "OIDC")
    {
        // Sign out from OIDC provider
        await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        await context.SignOutAsync("oidc-session");
    }

    // Clear JWT cookie
    context.Response.Cookies.Delete("app-jwt");

    return Results.Ok();
});
```

#### 3.4 Public Configuration Endpoint
Expose safe authentication config so frontend knows which login UI to render:

```csharp
app.MapGet("/api/auth/config", (IConfiguration config) =>
{
    var mode = config["Auth:Mode"] ?? "Basic";
    var oidcEnabled = config.GetValue<bool>("Auth:OIDC:Enabled", false);

    return Results.Ok(new
    {
        mode,
        basicAuthEnabled = mode == "Basic" || mode == "Mixed",
        oidcEnabled = oidcEnabled || mode == "OIDC" || mode == "Mixed",
        oidcProviderName = config["Auth:OIDC:ProviderName"] ?? "External",
        oidcLoginUrl = "/api/auth/external-login"
    });
});
```

### 4. Frontend Implementation (reporttree.client)

#### 4.1 Auth Store Updates (stores/auth.ts)
Add support for external authentication:

```typescript
// Add to useAuthStore
async function loginExternal() {
  // Redirect to backend OIDC challenge endpoint
  window.location.href = '/api/auth/external-login'
}

// Add route handler for /login/success
// This page should check for "app-jwt" cookie and store it
async function handleExternalLoginCallback() {
  const jwtCookie = document.cookie
    .split('; ')
    .find(row => row.startsWith('app-jwt='))
    ?.split('=')[1]

  if (jwtCookie) {
    setToken(jwtCookie)
    // Clean up cookie
    document.cookie = 'app-jwt=; Max-Age=0; path=/;'
    return true
  }
  return false
}
```

#### 4.2 Login View (Login.vue)
Refactor to support multiple modes.
- Fetch auth config on mount.
- If Mode == Basic: Show existing Username and Password form.
- If Mode == OIDC: Show a "Login with [ProviderName]" button.
  - Clicking the button redirects the browser to the backend challenge endpoint (for example, /api/auth/login).

Example UI wiring:

```vue
<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()

const authConfig = ref({
  mode: 'Basic',
  basicAuthEnabled: true,
  oidcEnabled: false,
  oidcProviderName: '',
  oidcLoginUrl: ''
})

onMounted(async () => {
  // Fetch auth configuration
  const config = await api.get('/auth/config', { skipAuth: true })
  authConfig.value = config
})

async function handleExternalLogin() {
  authStore.loginExternal()
}
</script>

<template>
  <!-- Show username/password form if Basic is enabled -->
  <form v-if="authConfig.basicAuthEnabled" @submit.prevent="handleBasicLogin">
    <!-- ... existing form fields ... -->
  </form>

  <!-- Show external login button if OIDC is enabled -->
  <cv-button 
    v-if="authConfig.oidcEnabled"
    kind="tertiary"
    @click="handleExternalLogin"
  >
    <template #icon><Login16 /></template>
    Login with {{ authConfig.oidcProviderName }}
  </cv-button>

  <div v-if="authConfig.basicAuthEnabled && authConfig.oidcEnabled" class="divider">
    or
  </div>
</template>
```

#### 4.3 Login Success Handler (views/LoginSuccess.vue)
Create a new page to handle the OIDC callback:

```vue
<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()

onMounted(async () => {
  const success = await authStore.handleExternalLoginCallback()
  if (success) {
    router.push('/')
  } else {
    router.push('/login?error=auth_failed')
  }
})
</script>

<template>
  <cv-loading :active="true" overlay>
    Completing authentication...
  </cv-loading>
</template>
```

Update router to include this route:

```typescript
{
  path: '/login/success',
  name: 'LoginSuccess',
  component: () => import('@/views/LoginSuccess.vue')
}
```

#### 4.4 Admin Settings UI
Since authentication is now configured via environment variables, the Admin UI will not support editing these settings.
- Optionally, add a read-only view in the Settings page to show the current configuration (masking secrets).

### 5. Migration Strategy

1. Configuration: Ensure appsettings.json or environment variables are set correctly in the deployment environment.
2. Backward Compatibility: Ensure existing local admin users can still log in.
   - Fallback: If OIDC is misconfigured, the admin can simply change the Auth__Mode env var back to Basic and restart the container.

### 6. Task Breakdown

#### Phase 1: Backend Foundation (2-3 days)
- [ ] Update AppUser model with AuthProvider and ExternalUserId fields
- [ ] Add GetByExternalIdAsync method to IUserRepository and implementation
- [ ] Create configuration binding classes for OIDC settings
- [ ] Create public API endpoint /api/auth/config
- [ ] Add NuGet packages: Microsoft.AspNetCore.Authentication.OpenIdConnect

#### Phase 2: OIDC Integration (3-4 days)
- [ ] Modify Program.cs to conditionally register OIDC services based on configuration
- [ ] Implement OnTokenValidated event handler with user sync logic
- [ ] Implement role mapping configuration and logic
- [ ] Add /api/auth/external-login and /api/auth/external-callback endpoints
- [ ] Update logout endpoint to handle OIDC sign-out
- [ ] Test with Azure AD test tenant

#### Phase 3: Frontend Updates (2-3 days)
- [ ] Update auth.ts store with external login methods
- [ ] Modify Login.vue to show conditional UI based on auth config
- [ ] Create LoginSuccess.vue component for OIDC callback
- [ ] Add router configuration for success callback
- [ ] Test end-to-end flow in dev environment

#### Phase 4: Testing and Documentation (2 days)
- [ ] Test with Azure AD (Entra ID)
- [ ] Test with generic OIDC provider (Auth0 and Okta)
- [ ] Test Mixed mode (both Basic and OIDC enabled)
- [ ] Test role mapping scenarios
- [ ] Document environment variables with examples for each provider
- [ ] Create admin guide for configuring OIDC
- [ ] Test fallback and error scenarios

#### Phase 5: Production Readiness (1 day)
- [ ] Update Docker Compose with environment variable examples
- [ ] Update Caddyfile if needed for callback URLs
- [ ] Create migration notes for existing deployments
- [ ] Security review of implementation

### 7. Provider-Specific Configuration Examples

#### Azure AD (Entra ID)
```bash
Auth__Mode=OIDC
Auth__OIDC__Enabled=true
Auth__OIDC__ProviderName="Azure AD"
Auth__OIDC__Authority=https://login.microsoftonline.com/{tenant-id}/v2.0
Auth__OIDC__ClientId={application-id}
Auth__OIDC__ClientSecret={client-secret}
Auth__OIDC__Scopes=openid profile email
Auth__OIDC__RoleClaimType=roles
Auth__OIDC__DefaultRole=Viewer
```

#### Okta
```bash
Auth__Mode=OIDC
Auth__OIDC__Enabled=true
Auth__OIDC__ProviderName="Okta"
Auth__OIDC__Authority=https://{your-domain}.okta.com
Auth__OIDC__ClientId={client-id}
Auth__OIDC__ClientSecret={client-secret}
Auth__OIDC__Scopes=openid profile email groups
Auth__OIDC__RoleClaimType=groups
Auth__OIDC__DefaultRole=Viewer
```

#### Auth0
```bash
Auth__Mode=OIDC
Auth__OIDC__Enabled=true
Auth__OIDC__ProviderName="Auth0"
Auth__OIDC__Authority=https://{tenant}.auth0.com
Auth__OIDC__ClientId={client-id}
Auth__OIDC__ClientSecret={client-secret}
Auth__OIDC__Scopes=openid profile email
Auth__OIDC__DefaultRole=Viewer
```

---

## Power BI Embedding Implementation Plan

### Overview
This document outlines the implementation plan for integrating Power BI embedding functionality into PBIHoster. The solution will support the "App owns the data" embedding model with both app secret and certificate-based authentication.

### Implementation Status
COMPLETED: All core phases implemented
Current Version: Fully functional Power BI embedding with dynamic workspace support

#### Key Implementation Highlights
- Backend: Complete Power BI API integration with RLS support
- Frontend: Dashboard components and dynamic workspace component
- Architecture: Component-based configuration (no Power BI fields in Page model)
- Features: Admin UI for settings, token refresh, audit logging

### Architecture Overview

#### Authentication Flow
```
1. Backend authenticates with Azure AD using app credentials
2. Backend obtains Power BI access token
3. Backend generates embed tokens for specific reports and dashboards
4. Frontend receives embed data and renders Power BI content
```

#### Key Components
- PowerBIService: Core service for Power BI API interactions
- PowerBIController: REST API endpoints for frontend
- PowerBIEmbedComponent: Vue component for rendering embedded content
- Configuration: Secure storage of Power BI credentials

---

### Phase 1: Backend Infrastructure (COMPLETED)

#### 1.1 NuGet Package Dependencies (COMPLETED)
Files modified: ReportTree.Server/ReportTree.Server.csproj

Installed packages:
```xml
<PackageReference Include="Microsoft.PowerBI.Api" Version="4.22.0" />
<PackageReference Include="Microsoft.Identity.Client" Version="4.79.2" />
```

#### 1.2 Configuration Model (COMPLETED)
Implemented: ReportTree.Server/Services/PowerBIConfiguration.cs

#### 1.3 DTOs for Power BI Data (COMPLETED)
Implemented: ReportTree.Server/DTOs/PowerBIDtos.cs

Created DTOs for:
- WorkspaceDto: Power BI workspace information
- ReportDto: Report metadata (Id, Name, EmbedUrl, DatasetId)
- DashboardDto: Dashboard metadata
- EmbedTokenRequestDto: Request parameters with RLS support (EnableRLS, RLSRoles)
- EmbedTokenResponseDto: Embed token, URL, and expiration details
- RLSIdentityDto: Username, roles, and datasets for Row Level Security

#### 1.4 Power BI Service Interface (COMPLETED)
Implemented: ReportTree.Server/Services/IPowerBIService.cs

#### 1.5 Power BI Service Implementation (COMPLETED)
Implemented: ReportTree.Server/Services/PowerBIService.cs

Features:
- MSAL authentication with token caching
- Support for both ClientSecret and Certificate authentication
- RLS (Row Level Security) support in embed token generation
- Workspace, report, and dashboard querying
- Thread-safe token refresh with SemaphoreSlim

#### 1.6 Power BI Controller (COMPLETED)
Implemented: ReportTree.Server/Controllers/PowerBIController.cs

Endpoints:
```csharp
[HttpGet("workspaces")] - Admin and Editor only
[HttpGet("workspaces/{workspaceId}/reports")] - All authenticated users
[HttpGet("workspaces/{workspaceId}/dashboards")] - All authenticated users
[HttpPost("embed/report")] - With page authorization and RLS support
[HttpPost("embed/dashboard")] - With page authorization
```

Key Features:
- Page-based authorization using PageAuthorizationService
- RLS parameters passed from component config
- Comprehensive audit logging via AuditLogService

#### 1.7 Configuration Storage (COMPLETED)
Implemented: Configuration via environment variables

Power BI settings configured via environment variables (not stored in database):
- PowerBI__TenantId
- PowerBI__ClientId
- PowerBI__ClientSecret
- PowerBI__AuthType (ClientSecret or Certificate)
- PowerBI__CertificateThumbprint (optional)
- PowerBI__CertificatePath (optional)
- PowerBI__AuthorityUrl (default: https://login.microsoftonline.com/{0}/)
- PowerBI__ResourceUrl (default: https://analysis.windows.net/powerbi/api)
- PowerBI__ApiUrl (default: https://api.powerbi.com)

Benefits:
- Follows twelve-factor app methodology
- Infrastructure as code
- No secrets in database
- Easy deployment across environments
- Consistent with other app configuration (JWT, CORS, etc.)

Service Registration: Program.cs registers IPowerBIService as Singleton (for token caching)

---

### Phase 2: Data Model Extensions (COMPLETED - Refactored)

#### 2.1 Page Model Design (COMPLETED)
Implemented: ReportTree.Server/Models/Page.cs

Architecture Decision: Power BI configuration stored in component config (Layout JSON), not Page model fields.

Optional field added:
```csharp
public Guid? PowerBIWorkspaceId { get; set; } // Convenience field for workspace-based pages
```

Benefits:
- Clean separation: Page handles navigation and layout
- Components handle their own configuration
- Multiple Power BI components per page supported
- No database schema coupling to Power BI

---

### Phase 3: Frontend Implementation (COMPLETED)

#### 3.1 TypeScript Types (COMPLETED)
Implemented: reporttree.client/src/types/powerbi.ts

All DTOs typed with RLS support in EmbedTokenRequestDto.

#### 3.2 Power BI Service (Frontend) (COMPLETED)
Implemented: reporttree.client/src/services/powerbi.service.ts

Methods:
- getWorkspaces()
- getReports(workspaceId)
- getDashboards(workspaceId)
- getReportEmbedToken(workspaceId, reportId, pageId?, enableRLS?, rlsRoles?)
- getDashboardEmbedToken(workspaceId, dashboardId, pageId?)

#### 3.3 Power BI Embed Component (COMPLETED)
Implemented: reporttree.client/src/components/PowerBIEmbed.vue

Features:
- Uses powerbi-client and powerbi-models (latest versions)
- Bootstrapping for faster load
- Event handling (loaded, rendered, error)
- Phased loading with spinner
- Proper cleanup on unmount

Dependencies installed:
```json
"powerbi-client": "^3.3.0",
"powerbi-models": "^2.1.0"
```

#### 3.8 Dashboard Components (COMPLETED)
Implemented:
- PowerBIReportComponent.vue - Report embed with RLS support and token refresh
- PowerBIReportComponentConfigure.vue - Configuration UI
- PowerBIDashboardComponent.vue - Dashboard embed
- PowerBIDashboardComponentConfigure.vue - Configuration UI
- PowerBIWorkspaceComponent.vue - Dynamic workspace with tabs for all reports

Component Config Types (src/types/components.ts):
```typescript
interface PowerBIReportComponentConfig {
    workspaceId?: string
    reportId?: string
    enableRLS?: boolean
    rlsRoles?: string[]
}

interface PowerBIDashboardComponentConfig {
    workspaceId?: string
    dashboardId?: string
}

interface PowerBIWorkspaceComponentConfig {
    workspaceId?: string
    enableRLS?: boolean
    rlsRoles?: string[]
}
```

Registration (src/config/components.ts):
- power-bi-report
- power-bi-dashboard
- power-bi-workspace - Dynamic workspace with tab navigation

#### 3.9 Feature: Workspace-Based Page (Refactored)
Concept: Instead of creating multiple pages (SyncWorkspace approach - removed), use dynamic component.

Implementation: PowerBIWorkspaceComponent.vue
- Fetches all reports from workspace at runtime
- Displays reports as tabs
- Uses ?reportId=xxx query parameter for navigation
- Auto-selects first report
- No database records needed for individual reports
- Always up-to-date with Power BI workspace

Benefits:
- No database bloat
- Always in sync with Power BI
- Dynamic report discovery
- Clean URL-based navigation

#### 3.7 Admin Settings UI (Removed)
Decision: Power BI credentials managed via environment variables, not Admin UI.

Rationale:
- Infrastructure configuration (not application data)
- Follows twelve-factor app principles
- More secure (no secrets in database)
- Consistent with JWT, CORS, and other settings
- Easier deployment and configuration management

---

### Phase 4: Security and Authorization (COMPLETED)

#### 4.1 Authorization Rules (COMPLETED)
Implemented in: PowerBIController.cs

- Page-based authorization via PageAuthorizationService
- RLS parameters passed from frontend component config
- Username from authenticated user identity
- Dataset ID fetched for RLS token generation
- Admin and Editor bypass for preview (no pageId required)

#### 4.2 Audit Logging (COMPLETED)
Implemented: Integration with AuditLogService

Logged events:
- Embed token generation (success and failure)
- Access denied attempts with context
- User identity and resource details

#### 4.3 Token Expiration Handling (COMPLETED)
Implemented: PowerBIReportComponent.vue

- Monitors token expiration timestamp
- Auto-refresh 5 minutes before expiration
- Schedules refresh using setTimeout
- Cleans up timers on unmount

#### 4.4 Error Handling (COMPLETED)
Backend:
- Exception logging in PowerBIService and PowerBIController
- Appropriate HTTP status codes (401, 403, 404)
- Detailed error context in logs

Frontend:
- User-friendly error messages
- Loading indicators
- Error state display in components

---

### Phase 5: Testing and Validation (PARTIAL)

#### 5.1 Backend Unit Tests (TODO)
Needs implementation

#### 5.2 Integration Tests (TODO)
Needs implementation

#### 5.3 Frontend Testing (Manual)
- Manual testing possible with configured workspace
- Token refresh tested
- Error scenarios handled

#### 5.4 Security Testing (COMPLETED)
- Client secret encryption verified
- Authorization enforcement tested
- Audit logging confirmed

---

### Phase 6: Documentation and Deployment (PARTIAL)

#### 6.1 Admin Documentation (TODO)
Needs update in README.md

#### 6.2 User Guide (TODO)
Needs creation

#### 6.3 Developer Documentation (THIS FILE)
Updated with implementation status

#### 6.4 Environment Variables (COMPLETED)
Implemented: .env.example updated with Power BI variables

Added to deployment configuration:
```bash
# Power BI Configuration
POWERBI_TENANT_ID=
POWERBI_CLIENT_ID=
POWERBI_CLIENT_SECRET=
POWERBI_AUTH_TYPE=ClientSecret
POWERBI_CERTIFICATE_THUMBPRINT=
POWERBI_CERTIFICATE_PATH=
POWERBI_AUTHORITY_URL=https://login.microsoftonline.com/{0}/
POWERBI_RESOURCE_URL=https://analysis.windows.net/powerbi/api
POWERBI_API_URL=https://api.powerbi.com
```

#### 6.5 Docker Configuration (COMPLETED)
Implemented: docker-compose.yml updated with Power BI env vars

Environment variables mapped to ASP.NET Core configuration:
```yaml
- PowerBI__TenantId=${POWERBI_TENANT_ID:-}
- PowerBI__ClientId=${POWERBI_CLIENT_ID:-}
- PowerBI__ClientSecret=${POWERBI_CLIENT_SECRET:-}
- PowerBI__AuthType=${POWERBI_AUTH_TYPE:-ClientSecret}
# ... and others
```

#### 6.6 Certificate Mounting (TODO)
Needs documentation

---

### Implementation Timeline - Actual

#### Week 1-2: Core Backend and Frontend (COMPLETED)
- Added NuGet packages (Microsoft.PowerBI.Api, Microsoft.Identity.Client)
- Created all models, DTOs, and services
- Implemented PowerBIService with MSAL auth and RLS
- Created PowerBIController with authorization
- Added Power BI settings to SettingsService
- Installed npm packages (powerbi-client, powerbi-models)
- Created TypeScript types and services
- Implemented PowerBIEmbed base component

#### Week 3: Dashboard Components (COMPLETED)
- Created PowerBIReportComponent with RLS and token refresh
- Created PowerBIDashboardComponent
- Created configuration components
- Implemented PowerBIWorkspaceComponent (dynamic workspace)
- Registered all components in component registry

#### Week 4: Admin UI and Refactoring (COMPLETED)
- Created PowerBISettings component (removed - using env vars instead)
- Integrated into Admin View (removed)
- Removed SyncWorkspace approach
- Refactored to component-based config architecture
- Made report and dashboard endpoints accessible to all users
- Added audit logging integration
- Refactored configuration to use environment variables

#### Remaining: Testing and Documentation (TODO)
- Unit tests
- Integration tests
- User documentation
- Deployment guide updates

---

### Architecture Decisions Made

#### 1. Component-Based Configuration (DECIDED)
Decision: Store Power BI config in component props (Layout JSON), not Page model fields.

Rationale:
- Clean separation of concerns
- Supports multiple Power BI components per page
- No schema coupling between Page and Power BI

#### 2. Dynamic Workspace Component (DECIDED)
Decision: Use PowerBIWorkspaceComponent instead of creating multiple Page records.

Rationale:
- No database bloat
- Always in sync with Power BI
- Dynamic discovery at runtime
- URL-based navigation (?reportId=xxx)

#### 3. RLS in Request (DECIDED)
Decision: Pass RLS config from frontend component to backend API.

Rationale:
- Component controls its own RLS settings
- Different components can have different RLS rules
- Username from authenticated user identity

#### 4. Token Refresh Strategy (DECIDED)
Decision: Frontend-driven token refresh 5 minutes before expiration.

Rationale:
- Seamless user experience
- No server-side websocket needed
- Component manages its own lifecycle

---

### Open Questions and Decisions Needed

#### 1. Certificate Storage (TODO)
Status: Not yet needed (using ClientSecret for now)
Recommendation: Document when needed

#### 2. Token Caching (DECIDED)
Decision: In-memory caching in PowerBIService (Singleton)
Status: Implemented with SemaphoreSlim for thread safety

#### 3. Embed Features (TODO)
Status: Basic features enabled
Future: Make configurable per component

#### 4. Workspace Selection (DECIDED)
Decision: Admin and Editor browse via Admin UI, all users can view embedded content
Status: Implemented

#### 5. Multi-Report Pages (DECIDED)
Decision: Use PowerBIWorkspaceComponent for multiple reports
Alternative: Multiple dashboard components on same page
Status: Both approaches supported

---

### Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Power BI API rate limits | High | Implement caching, monitor usage, add retry logic |
| Token expiration during use | Medium | Proactive refresh, user notifications |
| Large report load times | Medium | Loading indicators, lazy loading |
| Insufficient Power BI permissions | High | Clear documentation, validation at setup |
| Certificate management complexity | Medium | Provide multiple auth options, document thoroughly |
| Breaking changes in Power BI API | Low | Pin package versions, monitor deprecations |

---

### Success Criteria

- Users can browse Power BI workspaces and reports (Admin and Editor via Admin UI)
- Reports can be embedded via dashboard components (PowerBIReportComponent)
- Dashboards can be embedded via dashboard components (PowerBIDashboardComponent)
- Entire workspaces can be embedded with tab navigation (PowerBIWorkspaceComponent)
- Embedded reports render correctly with proper authentication
- Token refresh works seamlessly (5 minutes before expiration)
- Authorization prevents unauthorized access (PageAuthorizationService)
- RLS support: Row Level Security can be configured per component
- Audit logs capture all Power BI operations
- ClientSecret authentication works (Certificate support implemented but not tested)
- Admin UI for Power BI settings is functional
- Component-based architecture (no Power BI fields in Page model)
- Documentation needs completion
- Performance testing needed (under 2 seconds target for embed token)

---

### Future Enhancements (Post-MVP)

#### Phase 7: Advanced Features (TODO)
- Support for report bookmarks and saved views
- Custom filters and slicers configuration
- Export functionality (PDF, PowerPoint)
- Scheduled refresh monitoring
- Usage analytics and metrics
- Mobile layout optimization

#### Phase 8: Collaboration Features (TODO)
- Commenting on reports
- Sharing links with expiring tokens
- Email subscriptions for reports
- Report versioning and rollback

#### Phase 9: Performance Optimizations (TODO)
- Report thumbnail caching
- CDN integration for Power BI assets
- Progressive loading for large datasets
- Query caching strategies
- Redis-based token caching for horizontal scaling

#### Phase 10: Testing and Quality (TODO)
- Unit tests for PowerBIService
- Integration tests for PowerBIController
- Frontend component tests
- E2E testing with test workspace
- Load testing for token generation

---

### Dependencies

#### Azure Prerequisites
1. Azure AD App Registration
   - Client ID and Tenant ID
   - API Permissions: Report.Read.All, Workspace.Read.All, Dataset.Read.All
   - Client Secret or Certificate

2. Power BI Service
   - Power BI Pro or Premium license
   - Service Principal enabled in tenant settings
   - Workspaces with reports to embed

#### Development Tools
- Visual Studio Code or Visual Studio 2022
- .NET 10 SDK
- Node.js 20+ and npm
- Power BI Desktop (for creating test reports)
- Azure CLI (for app registration)

---

### Notes

- Follows existing code patterns and conventions in the project
- Uses Carbon Design System components for all UI
- Maintains consistent error handling across frontend and backend
- Sensitive data (ClientSecret) is encrypted at rest via SettingsService
- Comprehensive audit trail for compliance
- Performance target: under 2 seconds for embed token generation (needs measurement)
- Support for both light and dark themes in embedded reports (not yet configured)

---

### Implementation Summary

#### What is Working
1. Complete Backend Infrastructure
   - Power BI API integration with MSAL authentication
   - Token generation with RLS support
   - Page-based authorization
   - Audit logging

2. Complete Frontend Components
   - Base PowerBIEmbed component
   - Dashboard components (Report, Dashboard, Workspace)
   - Configuration UI for components
   - Admin settings panel

3. Key Features
   - Dynamic workspace component with tab navigation
   - Automatic token refresh (5 min before expiration)
   - Row Level Security (RLS) support
   - Component-based configuration architecture

#### What is Pending
1. Testing
   - Unit tests for backend services
   - Integration tests for controllers
   - Frontend component tests
   - E2E testing

2. Documentation
   - User guide for Power BI features
   - Admin documentation for Azure AD setup
   - Deployment guide updates
   - Troubleshooting guide

3. Performance
   - Benchmark embed token generation
   - Optimize API calls
   - Implement caching strategies

#### Quick Start Guide

For Administrators:
1. Configure Azure AD App with Power BI API permissions:
   - Report.Read.All
   - Workspace.Read.All
   - Dataset.Read.All
2. Set Environment Variables in .env file or hosting environment:
   ```bash
   POWERBI_TENANT_ID=your-tenant-id
   POWERBI_CLIENT_ID=your-client-id
   POWERBI_CLIENT_SECRET=your-client-secret
   POWERBI_AUTH_TYPE=ClientSecret
   ```
3. Deploy Application with updated environment variables
4. Add Components to Pages using the page editor

For Page Editors:
1. Navigate to page in edit mode
2. Add PowerBIReportComponent, PowerBIDashboardComponent, or PowerBIWorkspaceComponent
3. Configure workspace and report IDs in component settings
4. Optional: Configure RLS roles
5. Save and publish

For Users:
1. Navigate to pages with embedded Power BI content
2. Reports load automatically with your permissions
3. RLS is applied if configured
4. Tokens refresh automatically

---

### Approval and Next Steps

Implementation Status: Core features complete
Testing Status: Manual testing done, automated testing pending
Documentation Status: Technical docs complete, user docs pending

Next Actions:
1. Review and approve implementation - DONE
2. Set up Azure AD app registration - Admin responsibility
3. Complete core implementation - DONE
4. Write unit and integration tests
5. Create user documentation
6. Performance testing and optimization
7. Production deployment preparation

---

## Theme System Documentation

### Overview

The PBIHoster application now includes a comprehensive theme system based on Carbon Design System themes, with support for custom corporate themes.

### Features

- Built-in Themes: White, Gray 10, Gray 90, Gray 100 from Carbon Design System
- Custom Themes: Enterprise customers can create and apply custom corporate themes
- Theme Persistence: Selected theme is saved in localStorage
- Role-Based Management: Admin and Editor roles can create and manage custom themes
- Organization-Specific: Themes can be scoped to specific organizations

### User Guide

#### Switching Themes

1. Click the theme switcher icon in the header (top right)
2. Select from available themes:
   - White (default light theme)
   - Gray 10 (light theme with subtle gray background)
   - Gray 90 (dark theme)
   - Gray 100 (darker theme)
   - Any custom themes available to your organization

#### Creating Custom Themes (Admin and Editor Only)

1. Navigate to the Theme Manager (accessible from admin panel or tools)
2. Click "Create Custom Theme"
3. Fill in the required fields:
   - Theme Name: A descriptive name for your theme
   - Organization ID: (Optional) Leave empty for a global theme, or specify an organization ID
   - Theme Tokens: JSON object defining color tokens

4. Use the sample JSON as a starting point
5. Click "Save" to create the theme

#### Theme Token Structure

Custom themes use Carbon Design System v10 color tokens. Here is a minimal example:

```json
{
  "ui-background": "#ffffff",
  "ui-01": "#f4f4f4",
  "text-01": "#161616",
  "text-02": "#525252",
  "link-01": "#0f62fe",
  "interactive-01": "#0f62fe"
}
```

For a complete list of available tokens, refer to Carbon v10 documentation or the src/config/themes.ts file.

### Developer Guide

#### Frontend Architecture

Theme Store (src/stores/theme.ts)

The theme store manages:
- Current theme selection
- Custom theme data
- Theme application to the DOM
- API calls for theme CRUD operations

Key methods:
- setTheme(theme): Switch to a built-in theme
- setCustomTheme(theme): Apply a custom theme
- loadCustomThemes(): Fetch custom themes from API
- saveCustomTheme(theme): Create and update a custom theme
- deleteCustomTheme(id): Delete a custom theme
- initTheme(): Initialize theme on app start

#### Components

1. ThemeSwitcher.vue: Header dropdown for theme selection
2. ThemeManager.vue: Admin interface for managing custom themes

### Backend Architecture

#### Models (Models/CustomTheme.cs)

```csharp
public class CustomTheme
{
    public string Id { get; set; }
    public string Name { get; set; }
    public Dictionary<string, string> Tokens { get; set; }
    public bool IsCustom { get; set; }
    public string? OrganizationId { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

#### API Endpoints (Controllers/ThemesController.cs)

- GET /api/themes: Get all themes (optionally filtered by organizationId)
- GET /api/themes/{id}: Get a specific theme
- POST /api/themes: Create a new custom theme (Admin and Editor only)
- PUT /api/themes/{id}: Update a theme (Admin and Editor only)
- DELETE /api/themes/{id}: Delete a theme (Admin and Editor only)

#### Repository (Persistance/LiteDbThemeRepository.cs)

LiteDB-based persistence with methods for CRUD operations.

### Adding Custom Theme Support to Your Views

Themes are automatically applied at the document root level using CSS variables. To use theme colors in your components:

```vue
<style>
.my-component {
  background-color: var(--cds-ui-background);
  color: var(--cds-text-01);
}

.my-button {
  background-color: var(--cds-interactive-01);
  color: var(--cds-text-04);
}
</style>
```

### Theme Initialization

Themes are initialized in App.vue on mount:

```typescript
onMounted(async () => {
  // Initialize theme
  themeStore.initTheme()

  // Load custom themes if user is authenticated
  if (authStore.isAuthenticated) {
    await themeStore.loadCustomThemes()
  }
})
```

### Technical Details

#### How Themes Work

1. Built-in Themes: Carbon Design System provides four themes (white, g10, g90, g100) with predefined color tokens
2. CSS Variables: Theme tokens are applied as CSS custom properties (for example, --cds-background)
3. Theme Classes: The appropriate theme class (for example, cds--g90) is added to document.documentElement
4. Custom Themes: Override CSS variables with custom values while maintaining the structure

### Theme Storage

- Current Selection: Stored in localStorage as theme key
- Custom Theme Data: Stored in localStorage as customTheme key
- Backend Persistence: Custom themes are stored in LiteDB

### Security

- Authentication required for all theme API endpoints
- Only Admin and Editor roles can create, update, and delete themes
- Users can only delete themes they created (unless Admin)
- Organization-scoped themes ensure data isolation

### Best Practices

#### For Theme Creators

1. Start with an existing theme: Copy tokens from white, g10, g90, g100 and modify
2. Test accessibility: Ensure sufficient contrast ratios (WCAG AA minimum)
3. Use semantic naming: Token names should be semantic (for example, interactive-01) not descriptive (for example, blue)
4. Test all components: Preview your theme across all views before publishing
5. Document your theme: Include a description of when to use your custom theme

#### For Developers

1. Always use CSS variables: Use var(--cds-*) instead of hardcoded colors
2. Support all themes: Test your components with all built-in themes
3. Do not override theme variables: Let the theme system manage colors
4. Use Carbon components: Leverage @carbon/vue components which are theme-aware

### Troubleshooting

#### Theme Not Applying

1. Check browser console for errors
2. Verify theme is saved in localStorage
3. Clear browser cache and reload
4. Check that CSS variables are being set on document.documentElement

#### Custom Theme Not Saving

1. Verify user has Admin or Editor role
2. Check network tab for API errors
3. Validate JSON format of theme tokens
4. Ensure authentication token is valid

#### Colors Look Wrong

1. Verify all required tokens are defined
2. Check token names match Carbon conventions
3. Test with a built-in theme to isolate the issue
4. Review browser console for CSS warnings

### Future Enhancements

Potential improvements:
- Theme preview before applying
- Import and export themes as JSON
- Theme versioning and history
- Organization theme library
- Real-time theme sync across tabs
- Dark mode detection and auto-switching
- Theme marketplace for sharing themes
