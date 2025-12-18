# Power BI Embedding Implementation Plan

## Overview
This document outlines the implementation plan for integrating Power BI embedding functionality into PBIHoster. The solution will support the "App owns the data" embedding model with both app secret and certificate-based authentication.

## Architecture Overview

### Authentication Flow
```
1. Backend authenticates with Azure AD using app credentials
2. Backend obtains Power BI access token
3. Backend generates embed tokens for specific reports/dashboards
4. Frontend receives embed data and renders Power BI content
```

### Key Components
- **PowerBIService**: Core service for Power BI API interactions
- **PowerBIController**: REST API endpoints for frontend
- **PowerBIEmbedComponent**: Vue component for rendering embedded content
- **Configuration**: Secure storage of Power BI credentials

---

## Phase 1: Backend Infrastructure

### 1.1 NuGet Package Dependencies
**Files to modify**: `ReportTree.Server/ReportTree.Server.csproj`

Add the following packages:
```xml
<PackageReference Include="Microsoft.PowerBI.Api" Version="4.18.0" />
<PackageReference Include="Microsoft.Identity.Client" Version="4.61.0" />
<PackageReference Include="Microsoft.Rest.ClientRuntime" Version="2.3.24" />
```

### 1.2 Configuration Model
**New file**: `ReportTree.Server/Models/PowerBIConfiguration.cs`

```csharp
public class PowerBIConfiguration
{
    public string AuthorityUrl { get; set; }
    public string ResourceUrl { get; set; }
    public string ApiUrl { get; set; }
    public string TenantId { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string CertificateThumbprint { get; set; }
    public string CertificatePath { get; set; }
    public AuthenticationType AuthType { get; set; }
}

public enum AuthenticationType
{
    ClientSecret,
    Certificate
}
```

### 1.3 DTOs for Power BI Data
**New file**: `ReportTree.Server/DTOs/PowerBIDtos.cs`

Create DTOs for:
- `WorkspaceDto`: Power BI workspace information
- `ReportDto`: Report metadata (Id, Name, EmbedUrl, DatasetId)
- `DashboardDto`: Dashboard metadata
- `EmbedTokenRequestDto`: Request parameters for embed token generation
- `EmbedTokenResponseDto`: Embed token, URL, and expiration details
- `PowerBIResourceDto`: Generic resource info (type, id, name)

### 1.4 Power BI Service Interface
**New file**: `ReportTree.Server/Services/IPowerBIService.cs`

Define interface with methods:
```csharp
Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
Task<IEnumerable<WorkspaceDto>> GetWorkspacesAsync(CancellationToken cancellationToken = default);
Task<IEnumerable<ReportDto>> GetReportsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
Task<IEnumerable<DashboardDto>> GetDashboardsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
Task<EmbedTokenResponseDto> GetReportEmbedTokenAsync(Guid workspaceId, Guid reportId, CancellationToken cancellationToken = default);
Task<EmbedTokenResponseDto> GetDashboardEmbedTokenAsync(Guid workspaceId, Guid dashboardId, CancellationToken cancellationToken = default);
Task<ReportDto?> GetReportAsync(Guid workspaceId, Guid reportId, CancellationToken cancellationToken = default);
Task<DashboardDto?> GetDashboardAsync(Guid workspaceId, Guid dashboardId, CancellationToken cancellationToken = default);
```

### 1.5 Power BI Service Implementation
**New file**: `ReportTree.Server/Services/PowerBIService.cs`

Implement the service with:
- **Constructor**: Inject `IConfiguration`, `ILogger`, `ISettingsService`
- **GetAccessTokenAsync()**: 
  - Load configuration from SettingsService
  - Support both ClientSecret and Certificate authentication
  - Use MSAL (Microsoft.Identity.Client) to acquire token
  - Cache token with refresh logic
- **GetWorkspacesAsync()**: Query Power BI API for workspaces
- **GetReportsAsync()**: Get reports for a workspace
- **GetDashboardsAsync()**: Get dashboards for a workspace
- **GetReportEmbedTokenAsync()**: Generate embed token for report with View access level
- **GetDashboardEmbedTokenAsync()**: Generate embed token for dashboard
- Helper methods:
  - `CreatePowerBIClient(string accessToken)`: Factory for PowerBIClient
  - `LoadConfigurationAsync()`: Load settings from SettingsService
  - `AuthenticateWithSecretAsync()`: Secret-based auth
  - `AuthenticateWithCertificateAsync()`: Certificate-based auth

### 1.6 Power BI Controller
**New file**: `ReportTree.Server/Controllers/PowerBIController.cs`

Endpoints:
```csharp
[HttpGet("workspaces")]
[Authorize(Roles = "Admin,Editor")] // Only Admin/Editor can browse workspaces
Task<ActionResult<IEnumerable<WorkspaceDto>>> GetWorkspaces()

[HttpGet("workspaces/{workspaceId}/reports")]
[Authorize(Roles = "Admin,Editor")]
Task<ActionResult<IEnumerable<ReportDto>>> GetReports(Guid workspaceId)

[HttpGet("workspaces/{workspaceId}/dashboards")]
[Authorize(Roles = "Admin,Editor")]
Task<ActionResult<IEnumerable<DashboardDto>>> GetDashboards(Guid workspaceId)

[HttpPost("embed/report")]
[Authorize] // All authenticated users can request embed tokens for authorized reports
Task<ActionResult<EmbedTokenResponseDto>> GetReportEmbedToken(EmbedTokenRequestDto request)

[HttpPost("embed/dashboard")]
[Authorize]
Task<ActionResult<EmbedTokenResponseDto>> GetDashboardEmbedToken(EmbedTokenRequestDto request)

[HttpGet("workspaces/{workspaceId}/reports/{reportId}")]
[Authorize]
Task<ActionResult<ReportDto>> GetReport(Guid workspaceId, Guid reportId)
```

**Authorization Logic**:
- Check if user has access to the page that contains the Power BI resource
- Use `PageAuthorizationService` to verify permissions
- Return 403 if user doesn't have access

### 1.7 Configuration Storage
**Update**: Settings system to store Power BI configuration

Add settings via SettingsService with category "PowerBI":
- `PowerBI.TenantId` (not encrypted)
- `PowerBI.ClientId` (not encrypted)
- `PowerBI.ClientSecret` (encrypted)
- `PowerBI.AuthType` (values: "ClientSecret" or "Certificate")
- `PowerBI.CertificateThumbprint` (not encrypted, optional)
- `PowerBI.CertificatePath` (not encrypted, optional)
- `PowerBI.AuthorityUrl` (default: `https://login.microsoftonline.com/{tenantId}/`)
- `PowerBI.ResourceUrl` (default: `https://analysis.windows.net/powerbi/api`)
- `PowerBI.ApiUrl` (default: `https://api.powerbi.com`)

**Update**: `ReportTree.Server/Program.cs`
- Register `IPowerBIService` as Singleton (for token caching)
- Add configuration validation at startup

---

## Phase 2: Data Model Extensions

### 2.1 Page Model Extension
**Update**: `ReportTree.Server/Models/Page.cs`

Add properties:
```csharp
public string? PowerBIResourceType { get; set; } // "Report", "Dashboard", or null
public Guid? PowerBIWorkspaceId { get; set; }
public Guid? PowerBIResourceId { get; set; }
public string? PowerBIResourceName { get; set; } // Cache the name for display
```

### 2.2 Database Migration
**Considerations**:
- LiteDB will automatically add new fields when models are updated
- Existing pages will have null values for Power BI properties
- No manual migration needed, but document the schema change

### 2.3 Page Repository Update
**Update**: `ReportTree.Server/Persistance/IPageRepository.cs` and implementation
- No changes needed; existing CRUD methods will handle new properties

---

## Phase 3: Frontend Implementation

### 3.1 TypeScript Types
**New file**: `reporttree.client/src/types/powerbi.ts`

Define TypeScript interfaces matching DTOs:
```typescript
interface WorkspaceDto { id: string; name: string; }
interface ReportDto { id: string; name: string; embedUrl: string; datasetId: string; }
interface DashboardDto { id: string; name: string; embedUrl: string; }
interface EmbedTokenRequestDto { workspaceId: string; resourceId: string; resourceType: 'Report' | 'Dashboard'; }
interface EmbedTokenResponseDto { accessToken: string; embedUrl: string; tokenId: string; expiration: string; }
```

### 3.2 Power BI Service (Frontend)
**New file**: `reporttree.client/src/services/powerbi.service.ts`

API client with methods:
```typescript
async getWorkspaces(): Promise<WorkspaceDto[]>
async getReports(workspaceId: string): Promise<ReportDto[]>
async getDashboards(workspaceId: string): Promise<DashboardDto[]>
async getReportEmbedToken(workspaceId: string, reportId: string): Promise<EmbedTokenResponseDto>
async getDashboardEmbedToken(workspaceId: string, dashboardId: string): Promise<EmbedTokenResponseDto>
```

Include error handling and auth token injection from authStore.

### 3.3 Power BI Embed Component
**New file**: `reporttree.client/src/components/PowerBIEmbed.vue`

Component using `powerbi-client` library:
- **Props**: `embedUrl`, `accessToken`, `embedType` (report/dashboard), `reportId`
- **Features**:
  - Initialize Power BI embed on mount
  - Handle token refresh before expiration
  - Responsive container
  - Loading state
  - Error handling
  - Cleanup on unmount
- **Dependencies**: Install `powerbi-client` npm package

### 3.4 Power BI Browser Component (Admin/Editor)
**New file**: `reporttree.client/src/components/Admin/PowerBIBrowser.vue`

UI for browsing and selecting Power BI resources:
- Workspace selector (dropdown or list)
- Reports/Dashboards tabs
- Grid view with thumbnails (if available) or list view
- Select button to attach to page
- Search/filter functionality

### 3.5 Page Modal Updates
**Update**: `reporttree.client/src/components/PageModal.vue`

Add Power BI section:
- Checkbox: "Link Power BI Content"
- When checked, show:
  - Power BI Browser component
  - Selected resource display
  - Remove button
- Store selection in page model (workspaceId, resourceId, resourceType, resourceName)

### 3.6 Page View Updates
**Update**: `reporttree.client/src/views/PageView.vue` (or wherever pages are rendered)

Logic:
```typescript
if (page.powerBIResourceType && page.powerBIResourceId) {
  // Fetch embed token
  const embedData = await powerBIService.getReportEmbedToken(page.powerBIWorkspaceId, page.powerBIResourceId)
  // Render PowerBIEmbed component with embedData
} else {
  // Render dashboard layout (existing logic)
}
```

### 3.7 Admin Settings UI
**Update**: `reporttree.client/src/components/Admin/SettingsPanel.vue` (or create if missing)

Add Power BI Settings section:
- Form fields for:
  - Tenant ID
  - Client ID
  - Authentication Type (radio: Secret / Certificate)
  - Client Secret (password input, conditional on auth type)
  - Certificate Thumbprint (conditional)
  - Certificate Path (conditional)
- Save button calling settings API
- Test Connection button (calls backend to validate credentials)

---

## Phase 4: Security & Authorization

### 4.1 Authorization Rules
**Implementation in PowerBIController**:

- Users can only request embed tokens for pages they have access to
- Validation flow:
  1. Extract page from request (add `pageId` to `EmbedTokenRequestDto`)
  2. Call `PageAuthorizationService.CanAccessPageAsync(pageId, userId, userRoles, userGroups)`
  3. If false, return 403 Forbidden
  4. If true, proceed with embed token generation

### 4.2 Audit Logging
**Update**: `ReportTree.Server/Services/AuditLogService.cs`

Add audit log entries for:
- Power BI configuration changes
- Embed token requests (with userId, pageId, resourceType, resourceId)
- Access denied attempts

### 4.3 Token Expiration Handling
**Frontend**: 
- Monitor token expiration from `EmbedTokenResponseDto.expiration`
- Refresh token 5 minutes before expiration
- Show reconnecting indicator during refresh

### 4.4 Error Handling
**Backend**:
- Catch Power BI API exceptions (401, 403, 404, 429)
- Return appropriate HTTP status codes
- Log errors with context

**Frontend**:
- Display user-friendly error messages
- Retry logic for transient errors
- Fallback UI for embed failures

---

## Phase 5: Testing & Validation

### 5.1 Backend Unit Tests
**New files**: `ReportTree.Server.Tests/Services/PowerBIServiceTests.cs`

Test cases:
- Token acquisition (secret and certificate)
- Workspace retrieval
- Report/Dashboard retrieval
- Embed token generation
- Error scenarios (invalid credentials, missing workspace, etc.)

### 5.2 Integration Tests
**New files**: `ReportTree.Server.Tests/Controllers/PowerBIControllerTests.cs`

Test cases:
- Authorization enforcement
- Valid embed token requests
- Invalid requests (missing page access)
- CORS and security headers

### 5.3 Frontend Testing
- Manual testing with test Power BI workspace
- Token refresh flow
- Error scenarios (network failure, expired token)
- Responsive design on different screen sizes

### 5.4 Security Testing
- Verify encrypted storage of client secret
- Test authorization bypass attempts
- Validate CORS policies
- Check audit log completeness

---

## Phase 6: Documentation & Deployment

### 6.1 Admin Documentation
**Update**: `README.md`

Add section: "Power BI Integration"
- Prerequisites (Azure AD app registration)
- Configuration steps
- How to link reports to pages
- Troubleshooting guide

### 6.2 User Guide
**New file**: `docs/POWERBI_USER_GUIDE.md`

Content:
- How to browse Power BI workspaces
- Attaching reports to pages
- Managing permissions
- Best practices

### 6.3 Developer Documentation
**New file**: `docs/POWERBI_DEVELOPER_GUIDE.md`

Content:
- Architecture overview
- API reference
- Extension points
- Customization options

### 6.4 Environment Variables
**Update**: `.env.example` in deployment folder

Add:
```bash
# Power BI Configuration
POWERBI_TENANT_ID=your-tenant-id
POWERBI_CLIENT_ID=your-client-id
POWERBI_CLIENT_SECRET=your-client-secret
POWERBI_AUTH_TYPE=ClientSecret  # or Certificate
```

### 6.5 Docker Configuration
**Update**: `docker-compose.yml`

Add environment variables to `pbihoster` service:
```yaml
environment:
  - PowerBI__TenantId=${POWERBI_TENANT_ID}
  - PowerBI__ClientId=${POWERBI_CLIENT_ID}
  - PowerBI__ClientSecret=${POWERBI_CLIENT_SECRET}
  - PowerBI__AuthType=${POWERBI_AUTH_TYPE}
```

### 6.6 Certificate Mounting (for Certificate Auth)
**Update**: `docker-compose.yml`

Add volume mount for certificates:
```yaml
volumes:
  - ./certs:/app/certs:ro
```

Document certificate setup in deployment guide.

---

## Implementation Timeline

### Week 1: Backend Core
- [ ] Add NuGet packages
- [ ] Create models and DTOs
- [ ] Implement PowerBIService (token acquisition and API calls)
- [ ] Create PowerBIController with basic endpoints
- [ ] Add Power BI settings to SettingsService

### Week 2: Data Model & Authorization
- [ ] Extend Page model with Power BI properties
- [ ] Implement authorization logic in controller
- [ ] Add audit logging for Power BI operations
- [ ] Test backend endpoints with Postman/HTTP files

### Week 3: Frontend Core
- [ ] Add powerbi-client npm package
- [ ] Create TypeScript types
- [ ] Implement frontend PowerBI service
- [ ] Create PowerBIEmbed component
- [ ] Test embedding with sample report

### Week 4: Admin UI
- [ ] Create PowerBIBrowser component
- [ ] Update PageModal with Power BI linking
- [ ] Create Power BI settings panel in admin
- [ ] Implement test connection feature

### Week 5: Integration & Polish
- [ ] Update PageView to render embedded reports
- [ ] Implement token refresh logic
- [ ] Add error handling and loading states
- [ ] Responsive design adjustments

### Week 6: Testing & Documentation
- [ ] Write unit tests
- [ ] Perform integration testing
- [ ] Security audit
- [ ] Update documentation
- [ ] Create deployment guide updates

---

## Open Questions & Decisions Needed

### 1. Certificate Storage
**Question**: How should certificates be stored in production?
**Options**:
- File system mount (simple, requires manual management)
- Azure Key Vault (secure, requires Azure dependency)
- Environment variable (Base64 encoded)

**Recommendation**: Start with file system, document Azure Key Vault option.

### 2. Token Caching
**Question**: Should we cache Power BI access tokens in-memory or distributed cache?
**Options**:
- In-memory (simple, works for single instance)
- Redis (scalable, works for multiple instances)

**Recommendation**: In-memory for v1, add Redis support later if scaling horizontally.

### 3. Embed Features
**Question**: What Power BI embed features should be enabled by default?
**Options**: Filters panel, page navigation, export to PDF, etc.

**Recommendation**: Enable basic features, make configurable per page in future versions.

### 4. Workspace Selection
**Question**: Should users select workspaces/reports dynamically, or should admins pre-configure allowed workspaces?
**Options**:
- Dynamic (more flexible, requires more permissions)
- Pre-configured (more secure, less flexible)

**Recommendation**: Dynamic with admin-only access to browser. Regular users only see linked reports.

### 5. Multi-Report Pages
**Question**: Should a single page support multiple embedded reports?
**Current Model**: One Power BI resource per page.
**Future Enhancement**: Multiple resources via dashboard layout widgets.

**Recommendation**: Start with one-per-page, extend to multiple in Phase 7 (future).

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Power BI API rate limits | High | Implement caching, monitor usage, add retry logic |
| Token expiration during use | Medium | Proactive refresh, user notifications |
| Large report load times | Medium | Loading indicators, lazy loading |
| Insufficient Power BI permissions | High | Clear documentation, validation at setup |
| Certificate management complexity | Medium | Provide multiple auth options, document thoroughly |
| Breaking changes in Power BI API | Low | Pin package versions, monitor deprecations |

---

## Success Criteria

- [ ] Users can browse Power BI workspaces and reports (Admin/Editor)
- [ ] Reports can be linked to pages via PageModal
- [ ] Embedded reports render correctly with proper authentication
- [ ] Token refresh works seamlessly
- [ ] Authorization prevents unauthorized access
- [ ] Audit logs capture all Power BI operations
- [ ] Both ClientSecret and Certificate auth work
- [ ] Documentation is complete and accurate
- [ ] No security vulnerabilities introduced
- [ ] Performance is acceptable (<2s to load report)

---

## Future Enhancements (Post-MVP)

### Phase 7: Advanced Features
- Support for multiple reports per page (dashboard widgets)
- Report bookmarks and saved views
- Custom filters and slicers
- Export functionality (PDF, PowerPoint)
- Scheduled refresh monitoring
- Usage analytics

### Phase 8: Collaboration Features
- Commenting on reports
- Sharing links with expiring tokens
- Email subscriptions for reports
- Report versioning and rollback

### Phase 9: Performance Optimizations
- Report thumbnail caching
- CDN integration for Power BI assets
- Progressive loading for large datasets
- Query caching strategies

---

## Dependencies

### Azure Prerequisites
1. **Azure AD App Registration**
   - Client ID and Tenant ID
   - API Permissions: `Report.Read.All`, `Workspace.Read.All`, `Dataset.Read.All`
   - Client Secret or Certificate

2. **Power BI Service**
   - Power BI Pro or Premium license
   - Service Principal enabled in tenant settings
   - Workspaces with reports to embed

### Development Tools
- Visual Studio Code or Visual Studio 2022
- .NET 10 SDK
- Node.js 20+ and npm
- Power BI Desktop (for creating test reports)
- Azure CLI (for app registration)

---

## Notes

- Follow existing code patterns and conventions in the project
- Use Carbon Design System components for all UI
- Maintain consistent error handling across frontend and backend
- Ensure all sensitive data is encrypted at rest
- Keep audit trail comprehensive for compliance
- Performance target: <2 seconds for embed token generation
- Support both light and dark themes in embedded reports (Power BI setting)

---

## Approval & Next Steps

**Reviewed by**: [To be filled]  
**Approved by**: [To be filled]  
**Start Date**: [To be filled]  

**Next Actions**:
1. Review and approve this plan
2. Set up Azure AD app registration
3. Create feature branch: `feature/powerbi-embedding`
4. Begin Phase 1 implementation
5. Schedule weekly progress reviews
