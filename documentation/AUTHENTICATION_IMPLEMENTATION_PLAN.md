# Authentication Implementation Plan (Status: Implemented)

This document reflects the shipped Section 2 implementation for external authentication.

## Scope Status

- 2.1 OIDC/OAuth2 backend infrastructure: completed
- 2.2 External group sync: completed
- 2.3 Frontend SSO login flow: completed
- 2.4 Role mapping configuration: completed
- 2.x Admin non-secret mapping management: completed

## Implemented Architecture

PBIHoster uses a hybrid authentication model:

- Local auth: username/password -> local JWT
- External auth: OIDC challenge/callback -> local JWT

The API authorization model remains JWT-based for all protected endpoints.

## External Auth Endpoints

- GET /api/auth/external/providers
- GET /api/auth/external/challenge/{providerId}?returnUrl=/login
- GET /api/auth/external/callback/{providerId}?returnUrl=/login

Behavior:

- Challenge validates provider and starts provider-specific OIDC scheme.
- Callback reads short-lived external cookie, provisions/updates user, issues local JWT.
- Callback redirects to SPA using URL fragment:
  - success: #token=<jwt>
  - failure: #authError=<code>
- returnUrl is normalized to local relative paths to prevent open redirect attacks.

## Provider Configuration Model

External providers are declared in configuration under Security:ExternalAuth.

Secrets and protocol connection values remain configuration/environment managed:

- Authority
- ClientId
- ClientSecret
- CallbackPath
- RequireHttpsMetadata
- Scopes

## Admin-Managed Mapping Overrides (Non-Secret)

Admins can manage external mapping behavior without touching secrets via:

- GET /api/settings/external-auth/providers
- PUT /api/settings/external-auth/providers

Managed fields:

- DefaultRole
- GroupSyncEnabled
- GroupClaimType
- RemoveUnmappedGroupMemberships
- GroupMappings (ExternalGroup -> InternalGroup)
- RoleSyncEnabled
- RoleClaimType
- RoleMappings (ExternalRole -> InternalRole)

Persistence strategy:

- Overrides are stored in settings key Security.ExternalAuth.ProviderOverrides.
- Effective runtime configuration is produced by merging config/env providers with stored non-secret overrides.

## Claims Mapping Behavior

Role mapping:

- If RoleSyncEnabled is false: default role is used.
- If RoleSyncEnabled is true: mapped claim values resolve to internal roles.
- Internal roles are normalized and restricted to Admin, Editor, Viewer.
- If no mapped claim resolves: fallback to default role.

Group mapping:

- If GroupSyncEnabled is true: mapped claim values sync to internal groups.
- RemoveUnmappedGroupMemberships controls whether mapped groups not present in claims are removed.

## Frontend Implementation

Implemented UI and flow:

- Login view renders external provider buttons from /api/auth/external/providers.
- Auth callback route parses token/authError from URL fragment.
- Admin settings includes External Authentication section for non-secret mapping controls.

## Security Controls

- External provider discovery returns safe metadata only.
- Provider secrets are never exposed through admin APIs.
- Callback uses short-lived external cookie and clears it after use.
- returnUrl validation prevents open redirects.

## Tests and Validation

Covered by backend tests:

- ExternalAuthUrlValidatorTests
- ExternalGroupSyncServiceTests
- ExternalRoleMappingServiceTests
- ExternalAuthAdminSettingsIntegrationTests

Validation commands:

- dotnet build ReportTree.Server/ReportTree.Server.csproj
- dotnet test ReportTree.Server.Tests/ReportTree.Server.Tests.csproj
- npm run build (frontend)
