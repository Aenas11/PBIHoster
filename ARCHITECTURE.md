# Architecture Documentation

## System Overview

PBIHoster is an enterprise-grade Power BI hosting platform built with a modern, scalable architecture. The system follows a clear separation between the backend API, frontend SPA, and data persistence layers.

```
┌──────────────────────────────────────────────────────────────────┐
│                         Internet / Users                          │
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼
        ┌────────────────────────────────────┐
        │  Caddy Reverse Proxy / HTTPS       │
        │  - Certificate Management          │
        │  - Traffic Routing                 │
        │  - Security Headers                │
        └────────────┬───────────────────────┘
                     │
        ┌────────────▼───────────────────────┐
        │  ASP.NET Core Backend              │
        │  - REST API (Controllers)          │
        │  - Authentication & Authorization  │
        │  - Power BI Integration            │
        │  - Static SPA Serving              │
        └────────────┬───────────────────────┘
                     │
        ┌────────────▼───────────────────────────────────┐
        │           Data & External Services             │
        │  ┌─────────────────────────────────────────┐  │
        │  │  LiteDB (Embedded Database)             │  │
        │  │  - Users & Authentication               │  │
        │  │  - Page & Content Structure             │  │
        │  │  - Settings & Configuration             │  │
        │  │  - Audit Logs                           │  │
        │  └─────────────────────────────────────────┘  │
        │  ┌─────────────────────────────────────────┐  │
        │  │  External Services                      │  │
        │  │  - Azure AD / Power BI API              │  │
        │  │  - Email Service (optional)             │  │
        │  │  - Key Vault (optional)                 │  │
        │  └─────────────────────────────────────────┘  │
        └────────────────────────────────────────────────┘
```

## Layered Architecture

### Presentation Layer

**Technology**: Vue 3 + TypeScript + Vite  
**Location**: `reporttree.client/`

- **Components**: Reusable Vue components using Composition API + `<script setup>`
- **Views**: Page containers for major routes
- **Stores**: Pinia for state management (authentication, theme, etc.)
- **Services**: HTTP clients for API communication
- **Composables**: Reusable logic hooks

### API Layer

**Technology**: ASP.NET Core (.NET 10) Web API  
**Location**: `ReportTree.Server/`

The API follows a hybrid pattern:
- **Minimal APIs** (Program.cs): Authentication endpoints (login, register, logout)
- **Controllers**: Resource-based endpoints (pages, users, reports, settings)
- **Middleware**: Cross-cutting concerns (auth, logging, rate limiting, CORS)

#### API Structure
```
/api/auth/           - Authentication (login, register, refresh; logout is client-side)
/api/pages/          - Page/content management
/api/users/          - User management and profiles
/api/admin/          - Admin operations (requires Admin role)
/api/settings/       - Application settings
/api/themes/         - Theme management
/api/powerbi/        - Power BI integration
/api/refreshes/      - Dataset refresh management
/api/audit/          - Audit log queries
```

### Service Layer

**Purpose**: Business logic and external integrations

**Key Services**:
- **AuthService**: JWT generation, user authentication
- **TokenService**: JWT token operations
- **PageAuthorizationService**: Role-based access control for pages
- **PowerBIService**: Azure AD authentication and Power BI API calls
- **SettingsService**: Configuration management with encryption
- **AuditLogService**: Comprehensive logging of user actions
- **BrandingService**: Logo and custom theme management
- **RefreshSchedulerHostedService**: Background job scheduler for dataset refreshes

### Data Access Layer (Repository Pattern)

**Technology**: LiteDB with Repository Pattern  
**Location**: `ReportTree.Server/Persistance/`

Each entity has a corresponding repository:
```csharp
IUserRepository      // AppUser operations
IPageRepository      // Page/content operations
ISettingRepository   // Settings operations
IAuditLogRepository  // Audit logging
IThemeRepository     // Custom themes
IGroupRepository     // User groups
```

### Data Model

#### Core Entities

**AppUser**
```csharp
public class AppUser
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string? PasswordHash { get; set; }  // null for external users
    public List<string> Roles { get; set; }     // Admin, Editor, Viewer
    public List<Guid> FavoritePageIds { get; set; }
    public string? AuthProvider { get; set; }   // Local, AzureAd, Okta, etc.
    public string? ExternalUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsLocked { get; set; }
}
```

**Page**
```csharp
public class Page
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }           // Carbon Design icon name
    public List<string> AllowedRoles { get; set; }
    public bool IsPublic { get; set; }
    public List<Guid> ChildPageIds { get; set; }
    public Guid? ParentPageId { get; set; }
    public PageLayout? Layout { get; set; }     // Drag-drop layout config
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
}
```

**AppSetting**
```csharp
public class AppSetting
{
    public string Key { get; set; }
    public string Value { get; set; }           // Encrypted if sensitive
    public string Category { get; set; }        // General, Security, PowerBI, Email
    public string Description { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid UpdatedByUserId { get; set; }
}
```

**AuditLog**
```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public string Action { get; set; }          // LOGIN, CREATE_PAGE, DELETE_USER, etc.
    public string Resource { get; set; }        // User, Page, Settings, etc.
    public string? ResourceId { get; set; }
    public Guid? UserId { get; set; }
    public string? IpAddress { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Additional Entities**: Group, BrandingAsset, CustomTheme, LoginAttempt, DatasetRefreshSchedule, DatasetRefreshRun

## Authentication & Authorization

### Authentication Flow

1. **Local Authentication** (Default)
   ```
   User enters credentials → Backend validates → JWT generated → Frontend stores token
   ```

2. **External Authentication** (OIDC/OAuth2 - Optional)
   ```
   User initiates login → External provider flow → Backend exchanges code for ID token
   → JWT generated → Session established
   ```

### Authorization Model

**Role-Based Access Control (RBAC)**
```
Admin   → Full access, user/settings management
Editor  → Create/edit content, manage pages
Viewer  → Read-only access to assigned pages
Public  → No authentication required (specific pages only)
```

**Page-Level Access Control**
- Whitelist by role (Admin, Editor, Viewer)
- Whitelist by individual user
- Whitelist by group membership
- Toggle public access (no auth required)

### JWT Token Structure

```json
{
  "sub": "user-id",
  "username": "john.doe",
  "email": "john@example.com",
  "roles": ["Admin", "Editor"],
  "groups": ["engineering", "leadership"],
  "iat": 1234567890,
  "exp": 1234571490
}
```

## Power BI Integration

### Embedding Model: "App Owns the Data"

```
┌─────────────┐
│   Backend   │
└──────┬──────┘
       │ 1. App authenticates with Azure AD
       │    (ClientSecret or Certificate)
       ▼
┌─────────────────────────┐
│     Azure AD            │
│  Returns Access Token   │
└──────┬──────────────────┘
       │ 2. App requests embed token
       │    (specific report + user identity + RLS)
       ▼
┌─────────────────────────┐
│   Power BI API          │
│  Returns Embed Token    │
└──────┬──────────────────┘
       │ 3. Returns to frontend
       │    (token + report metadata)
       ▼
┌─────────────┐
│  Frontend   │ 4. Renders Power BI report using token
└─────────────┘
```

### RLS (Row-Level Security) Support

When generating embed tokens, the backend can specify:
- **Username**: Identity for RLS filtering
- **Roles**: User roles for RLS rules in Power BI

Example:
```csharp
var embedToken = await _powerBIService.GenerateEmbedToken(
    reportId: new Guid("..."),
    username: "john.doe",
    roles: new[] { "Sales_Team", "North_Region" }  // RLS roles in Power BI
);
```

## Security Architecture

### Layers of Defense

```
Layer 1: Perimeter
├─ HTTPS/TLS (Caddy reverse proxy)
├─ Security Headers (X-Frame-Options, CSP, etc.)
└─ CORS protection

Layer 2: Authentication
├─ JWT Bearer tokens
├─ Token expiration (8 hours default)
├─ Password complexity requirements
└─ Account lockout after failed attempts

Layer 3: Authorization
├─ Role-based access control
├─ Page-level permissions
├─ Endpoint authorization checks
└─ External service least privilege

Layer 4: Data Protection
├─ Encrypted passwords (BCrypt)
├─ Encrypted sensitive settings
├─ No sensitive data in logs
└─ Database file protection

Layer 5: Monitoring
├─ Comprehensive audit logging
├─ Rate limiting (429 responses)
├─ Brute force prevention
└─ Health checks & alerting
```

### Key Security Features

**Pre-Authentication**
- Rate limiting on auth endpoints (5 req/min)
- CORS validation
- Security headers on all responses

**Authentication**
- JWT Bearer token validation
- Token expiration enforcement
- Optional multi-factor via external provider (OIDC)

**Authorization**
- Route-level `[Authorize(Roles = "...")]` attributes
- Custom `PageAuthorizationService` for content access
- Claims-based authorization

**Post-Authentication**
- Audit log all actions
- Encrypt sensitive settings
- Rate limit general API (100 req/min)

## Deployment Architecture

### Docker Deployment

```yaml
services:
  pbihoster:                    # Single container
    ├─ ASP.NET Core backend
    ├─ Vue.js frontend (static files)
    └─ LiteDB database (mounted volume)

  caddy:                        # Reverse proxy
    ├─ HTTPS/TLS termination
    ├─ Automatic certificate renewal
    └─ Routes traffic to pbihoster
```

### Kubernetes Deployment

⚠️ **Important**: LiteDB is a file-based embedded database and **does not support concurrent access from multiple processes**. This section explains the limitations and recommended deployment patterns.

#### Single Replica Deployment (Recommended for LiteDB)

```yaml
Deployment: pbihoster
├─ Replicas: 1 (required for LiteDB file safety)
├─ Readiness probe: /ready (LiteDB connectivity)
├─ Liveness probe: /health (process alive)
├─ Persistent volume: /data (LiteDB file - ReadWriteOnce required)
└─ Environment: ConfigMap + Secrets

Service: pbihoster
├─ Type: ClusterIP (internal)
└─ Port: 8080

Ingress: (external access)
├─ TLS termination
├─ Certificate management
└─ Host-based routing
```

**Why single replica?**
- LiteDB uses exclusive file locks (only one process can write)
- Multiple replicas sharing one PersistentVolume would conflict
- Multiple replicas with separate PVs would have inconsistent data
- Concurrent writes cause corruption and data loss

**High Availability with LiteDB:**
- Use managed PersistentVolume snapshots for rapid recovery
- Implement automated daily backups to object storage
- Configure PVC on RWO (ReadWriteOnce) volume with automatic failover
- Use pod anti-affinity to prevent same-node scheduling in single-replica setup

#### Multi-Replica Deployment (Requires Database Migration)

For true high availability with multiple replicas, migrate to a networked database:

```yaml
# Step 1: Add database abstraction layer
Services:
├─ PostgreSQL or MySQL (replicated/managed)
└─ PBIHoster instances (2-3 replicas, stateless)

# Step 2: Update connection strings
Replicas: 2-3
Database: PostgreSQL/MySQL (external, replicated)
PersistentVolume: Not needed (stateless)

Benefits:
├─ True horizontal scaling
├─ Data consistency across replicas
├─ Built-in replication and backup
└─ Managed services available (RDS, CloudSQL, etc.)
```

**Migration effort**: Medium  
**Timeline**: Planned for v1.0.0  
**Current recommendation**: Stay with single-replica LiteDB deployment for now

## Configuration Management

### Priority Order

1. **Environment Variables** (Highest - runtime)
2. **appsettings.{Environment}.json** (Build-time)
3. **Azure Key Vault** (If `KEY_VAULT_URI` set)
4. **Database Settings** (AppSetting collection)

### Environment Variables by Category

**Core**
- `ASPNETCORE_ENVIRONMENT` (Development, Production)
- `PORT` (Default: 8080)

**Security**
- `JWT_KEY` (Required, 256+ bits)
- `JWT_ISSUER`, `JWT_EXPIRY_HOURS`
- `PASSWORD_*` (complexity rules)
- `RATE_LIMIT_*`
- `CORS_ORIGIN_*`, `CORS_ALLOW_CREDENTIALS`

**Power BI**
- `POWERBI_TENANT_ID`, `POWERBI_CLIENT_ID`
- `POWERBI_CLIENT_SECRET` or `POWERBI_CERTIFICATE_*`
- `POWERBI_AUTH_TYPE`, `POWERBI_AUTHORITY_URL`, etc.

**Optional**
- `KEY_VAULT_URI` (Azure Key Vault integration)
- `VITE_MONITORING_ENDPOINT` (Frontend error reporting)

## Monitoring & Observability

### Logs

- **Format**: Structured JSON via Serilog
- **Enrichment**: CorrelationId, MachineName, ProcessId, ThreadId
- **Destinations**: stdout (container logs), optional aggregator (Loki, Elasticsearch, Azure Monitor)

### Metrics

- **Format**: Prometheus
- **Endpoint**: `/metrics`
- **Scrapers**: Prometheus, Grafana, Azure Monitor, etc.
- **Instruments**: ASP.NET Core, HTTP client, runtime (GC, memory, CPU)

### Health Checks

```
GET /health      → 200 OK (liveness - process up)
GET /ready       → 200 OK (readiness - LiteDB accessible)
GET /metrics     → Prometheus format (metrics)
```

## Development Workflow

### Local Development

1. **Backend**: `dotnet watch run` (hot reload on changes)
2. **Frontend**: `npm run dev` (Vite dev server + API proxy)
3. **Database**: Automatic initialization at startup

### Deployment

1. **Build**: `dotnet publish` (includes frontend build via npm)
2. **Docker**: `docker build` (containerizes everything)
3. **Deploy**: `docker-compose up` or Kubernetes `kubectl apply`

---

## Related Documentation

- [API.md](API.md) - REST API endpoints and authentication
- [DATABASE.md](DATABASE.md) - LiteDB schema and queries
- [SECURITY.md](SECURITY.md) - Security implementation details
- [DEPLOYMENT.md](deployment/DEPLOYMENT.md) - Production deployment guide
- [ROADMAP.md](ROADMAP.md) - Feature roadmap and implementation plans
