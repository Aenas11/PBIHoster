# Authentication Implementation Plan

This document outlines the plan to extend the current authentication system ("Basic Authentication") to support external identity providers via OAuth2/OpenID Connect (OIDC), such as Azure AD (Entra ID), Okta, Auth0, and Clerk.

## 1. Architecture Overview

The system will support two primary modes of authentication, configurable by the administrator:

1.  **Basic Authentication** (Current): Local username/password stored in LiteDB with BCrypt hashing + JWT tokens.
2.  **External Authentication** (New): OAuth2/OIDC based authentication where an external provider handles identity verification.

### Authentication Flow Decision

**Hybrid Approach** (Recommended):
- **OIDC providers** issue authentication cookies via standard OpenID Connect flow
- **After successful OIDC auth**, backend generates internal JWT token (same format as Basic auth)
- **Frontend** uses JWT for all API calls (consistent with current implementation)
- **Benefits**: 
  - No frontend changes to API client (still uses Bearer tokens)
  - OIDC session management handled server-side
  - Supports token refresh without re-authenticating
  - Compatible with SPA architecture

### Key Concepts
-   **Configuration**: The application will read authentication settings from **Environment Variables** (or `appsettings.json`) at startup.
-   **User Mapping**: External users will be mapped to internal `AppUser` records (JIT sync).
    -   **Auto-provisioning**: Users authenticating successfully via an external provider will have a local account created automatically if one doesn't exist.
    -   **Lightweight Sync**: Only store essential data (ExternalUserId, Email, Roles). `PasswordHash` remains `null` for external users.
    -   **Update on Login**: Refresh email/display name from claims on each login to maintain consistency.
-   **Role Mapping**: 
    -   **Option 1 (Simple)**: Assign default `Viewer` role on first login, admins manually promote users
    -   **Option 2 (Advanced)**: Map external claims/groups to internal roles via configuration (e.g., `AzureAD_Group_Admins` → `Admin`)
-   **Mixed Mode Support**: System can have both local and external users simultaneously (identified by `AuthProvider` field).

## 2. Database Schema Changes

### 2.1. AppUser Model
Extend `AppUser` to support external identity linking.

```csharp
public class AppUser
{
    // ... existing properties ...
    
    // New properties
    public string? AuthProvider { get; set; } // "Local", "AzureAd", "Okta", etc.
    public string? ExternalUserId { get; set; } // Subject ID from the provider
}
```

### 2.2. Configuration (Environment Variables)
Authentication is configured via standard ASP.NET Core configuration providers (Environment Variables, `appsettings.json`).

| Environment Variable | Description | Example |
| --- | --- | --- |
| `Auth__Mode` | The active authentication mode. | `Basic`, `OIDC`, or `Mixed` |
| `Auth__OIDC__Enabled` | Enable OIDC authentication. | `true` or `false` |
| `Auth__OIDC__ProviderName` | Display name for the login button. | `Azure AD`, `Okta` |
| `Auth__OIDC__Authority` | The issuer URL. | `https://login.microsoftonline.com/{tenant}/v2.0` |
| `Auth__OIDC__ClientId` | The client ID. | `GUID` |
| `Auth__OIDC__ClientSecret` | The client secret. | `secret-value` |
| `Auth__OIDC__ResponseType` | OAuth flow response type. | `code` (default, most secure) |
| `Auth__OIDC__Scopes` | Scopes to request. | `openid profile email` |
| `Auth__OIDC__CallbackPath` | The redirect URI path. | `/signin-oidc` |
| `Auth__OIDC__SignedOutCallbackPath` | Post-logout redirect path. | `/signout-callback-oidc` |
| `Auth__OIDC__RoleClaimType` | Claim to use for role mapping. | `roles`, `groups`, or `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` |
| `Auth__OIDC__DefaultRole` | Default role for new external users. | `Viewer` |
| `Auth__OIDC__RoleMappings__*` | Map external groups to roles. | `Auth__OIDC__RoleMappings__AdminGroup=Admin` |

**Note**: When `Auth__Mode` is `Mixed`, both Basic and OIDC are available. When `Basic`, only local auth works. When `OIDC`, external auth is primary but local admin fallback can still be enabled.

## 3. Backend Implementation (`ReportTree.Server`)

### 3.1. Authentication Configuration (`Program.cs`)
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
                   
                   // Configure from Environment Variables / appsettings.json
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
        // Update existing user (refresh email/name)
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
    
    // Get external groups/roles
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

### 3.2. Repository Updates
Add method to `IUserRepository` and `LiteDbUserRepository`:

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

### 3.3. Controller Updates
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

### 3.4. Public Configuration Endpoint
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
### 4.1. Auth Store Updates (`stores/auth.ts`)
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
## 6. Security Considerations

### 6.1. Token Security
-   OIDC tokens should be stored in `HttpOnly` cookies during the authentication flow
-   Internal JWT tokens are exposed to JavaScript (for API calls) but have short expiry (8 hours)
-   Implement token refresh mechanism if needed

### 6.2. Role Escalation Prevention
-   External role mappings should be validated and sanitized
-   Admin role should never be auto-assigned from external claims (unless explicitly configured)
-   Maintain audit log of role changes

### 6.3. Fallback Admin Access
-   Always keep at least one local admin account for emergency access
-   If OIDC misconfigured, system should allow falling back to Basic auth
-   Consider adding `Auth__ForceBasicAuth` emergency override setting

### 6.4. CSRF Protection
-   OIDC flow includes state parameter for CSRF protection (handled by middleware)
-   Ensure `SameSite=Strict` on auth cookies

## 7. Task Breakdown

### Phase 1: Backend Foundation (2-3 days)
- [ ] Update `AppUser` model with `AuthProvider` and `ExternalUserId` fields
- [ ] Add `GetByExternalIdAsync` method to `IUserRepository` and implementation
- [ ] Create configuration binding classes for OIDC settings
- [ ] Create public API endpoint `/api/auth/config`
- [ ] Add NuGet packages: `Microsoft.AspNetCore.Authentication.OpenIdConnect`

### Phase 2: OIDC Integration (3-4 days)
- [ ] Modify `Program.cs` to conditionally register OIDC services based on configuration
- [ ] Implement `OnTokenValidated` event handler with user sync logic
- [ ] Implement role mapping configuration and logic
- [ ] Add `/api/auth/external-login` and `/api/auth/external-callback` endpoints
- [ ] Update logout endpoint to handle OIDC sign-out
- [ ] Test with Azure AD test tenant

### Phase 3: Frontend Updates (2-3 days)
- [ ] Update `auth.ts` store with external login methods
- [ ] Modify `Login.vue` to show conditional UI based on auth config
- [ ] Create `LoginSuccess.vue` component for OIDC callback
- [ ] Add router configuration for success callback
- [ ] Test end-to-end flow in dev environment

### Phase 4: Testing & Documentation (2 days)
- [ ] Test with Azure AD (Entra ID)
- [ ] Test with generic OIDC provider (Auth0/Okta)
- [ ] Test Mixed mode (both Basic and OIDC enabled)
- [ ] Test role mapping scenarios
- [ ] Document environment variables with examples for each provider
- [ ] Create admin guide for configuring OIDC
- [ ] Test fallback/error scenarios

### Phase 5: Production Readiness (1 day)
- [ ] Update Docker Compose with environment variable examples
- [ ] Update Caddyfile if needed for callback URLs
- [ ] Create migration notes for existing deployments
- [ ] Security review of implementation

## 8. Provider-Specific Configuration Examples

### Azure AD (Entra ID)
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

### Okta
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

### Auth0
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

### 4.3. Login Success Handler (`views/LoginSuccess.vue`)
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
## 4. Frontend Implementation (`reporttree.client`)

### 4.1. Login View (`Login.vue`)
Refactor to support multiple modes.
-   Fetch auth config on mount.
-   **If Mode == Basic**: Show existing Username/Password form.
-   **If Mode == OIDC**: Show a "Login with [ProviderName]" button.
    -   Clicking the button redirects the browser to the backend challenge endpoint (e.g., `/api/auth/login`).

### 4.2. Admin Settings UI
Since authentication is now configured via environment variables, the Admin UI will not support editing these settings.
-   Optionally, add a "Read-Only" view in the Settings page to show the current configuration (masking secrets).

## 5. Migration Strategy

1.  **Configuration**: Ensure `appsettings.json` or environment variables are set correctly in the deployment environment.
2.  **Backward Compatibility**: Ensure existing local admin users can still log in.
    -   *Fallback*: If OIDC is misconfigured, the admin can simply change the `Auth__Mode` env var back to `Basic` and restart the container.

## 6. Task Breakdown

### Phase 1: Backend Foundation
- [ ] Update `AppUser` model.
- [ ] Create `AuthenticationSettings` class (to bind configuration).
- [ ] Create public API endpoint `/api/auth/config`.

### Phase 2: OIDC Integration
- [ ] Modify `Program.cs` to conditionally register OIDC services based on `IConfiguration`.
- [ ] Implement `ExternalLoginCallback` logic to provision users in LiteDB.
- [ ] Implement JWT generation for external users (if we keep using JWTs for API access after OIDC login) OR switch to Cookie Auth for everything.
    -   *Recommendation*: Keep using JWTs for the API. The OIDC flow can issue a cookie, and then the frontend can exchange that cookie (or the OIDC tokens) for the app's internal JWT.

### Phase 3: Frontend Updates
- [ ] Update `Login.vue` to handle dynamic auth modes.

### Phase 4: Testing & Documentation
- [ ] Test with Azure AD (Entra ID).
- [ ] Test with a generic OIDC provider (e.g., Auth0 or Keycloak).
- [ ] Document environment variables for admins.
