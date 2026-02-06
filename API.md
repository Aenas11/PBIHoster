# API Documentation

## Overview

PBIHoster exposes a REST API for all frontend operations and integrations. The API uses JWT Bearer authentication and follows RESTful conventions.

**Base URL**: `/api/`  
**Authentication**: JWT Bearer Token (except public endpoints)  
**Response Format**: JSON

## Authentication

### JWT Bearer Token

All endpoints (except login/register) require authentication via `Authorization: Bearer <token>` header.

```http
GET /api/pages HTTP/1.1
Authorization: Bearer eyJhbGc...
```

### Token Acquisition

#### 1. Register a New User

```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "john.doe",
  "password": "SecurePass123!",
  "roles": ["Viewer"]
}

Response 200 OK
```

#### 2. Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "john.doe",
  "password": "SecurePass123!"
}

Response 200 OK:
{
  "token": "eyJhbGc..."
}

Response 400 Bad Request (invalid credentials):
{
  "error": "Invalid username or password"
}

Response 400 Bad Request (account lockout):
{
  "error": "Account is locked. Try again in 15 minutes."
}
```

#### 3. Logout (Client-side)

There is no server-side logout endpoint. Clients should delete the stored JWT token to sign out.

### Token Expiration & Refresh

Tokens expire after configured duration (default: 8 hours). Use the refresh endpoint to obtain a new JWT before expiry.

## Endpoints by Resource

### Authentication (`/auth`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/auth/login` | ❌ No | Login with username/password |
| POST | `/auth/register` | ❌ No | Register new user (first user becomes Admin) |
| POST | `/auth/logout` | ✅ Yes | **Not implemented** (client-side logout only) |
| POST | `/auth/refresh` | ✅ Yes | Refresh JWT using current session |

### Pages (`/pages`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/pages` | ❌ No | Public/All | List accessible pages (auth applied if logged in) |
| GET | `/pages/{pageId}` | ❌ No | Public/All | Get page details (auth required if not public) |
| POST | `/pages` | ✅ Yes | Admin, Editor | Create new page |
| PUT | `/pages/{pageId}` | ✅ Yes | Admin, Editor | Update page metadata/layout |
| DELETE | `/pages/{pageId}` | ✅ Yes | Admin, Editor | Delete page |
| POST | `/pages/{pageId}/layout` | ✅ Yes | Admin, Editor | Save layout JSON for page |
| POST | `/pages/{pageId}/clone` | ✅ Yes | Admin, Editor | Clone page (optionally set new title/parent) |

#### Get Pages (Accessible)

```http
GET /api/pages
Authorization: Bearer <token>

Response 200 OK:
[
  {
    "id": 1,
    "title": "Sales Dashboard",
    "icon": "dashboard",
    "parentId": null,
    "isPublic": false,
    "allowedUsers": ["alice", "bob"],
    "allowedGroups": ["Sales"],
    "layout": {
      "components": [
        {
          "id": "comp1",
          "type": "PowerBIReport",
          "position": { "x": 0, "y": 0, "w": 12, "h": 6 },
          "config": {
            "reportId": "...",
            "workspaceId": "..."
          }
        }
      ]
    }
  }
]
```

#### Create Page

```http
POST /api/pages
Authorization: Bearer <token>
Content-Type: application/json

{
  "title": "Sales Dashboard",
  "icon": "chart--column",
  "isPublic": false
}

Response 201 Created:
{
  "id": 101,
  "title": "Sales Dashboard",
  ...
}
```

### Users & Profile (`/admin/users`, `/profile`, `/directory`)

**Admin User Management** (`/admin/users`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/admin/users` | ✅ Yes | Admin | Search users (query param `term`) |
| POST | `/admin/users` | ✅ Yes | Admin | Create or update a user (roles, password, groups) |
| DELETE | `/admin/users/{username}` | ✅ Yes | Admin | Delete user |

**Profile** (`/profile`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/profile` | ✅ Yes | All | Get current user profile |
| PUT | `/profile` | ✅ Yes | All | Update profile (email) |
| POST | `/profile/change-password` | ✅ Yes | All | Change own password |

**Favorites & Recents** (`/profile`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/profile/favorites` | ✅ Yes | All | Get favorite page IDs |
| POST | `/profile/favorites/{pageId}` | ✅ Yes | All | Add favorite page |
| DELETE | `/profile/favorites/{pageId}` | ✅ Yes | All | Remove favorite page |
| GET | `/profile/recent` | ✅ Yes | All | Get recent page IDs |
| POST | `/profile/recent/{pageId}` | ✅ Yes | All | Record recently viewed page |

**Directory Search** (`/directory`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/directory/users` | ✅ Yes | All | Search users (query param `query`) |
| GET | `/directory/groups` | ✅ Yes | All | Search groups (query param `query`) |

### User Groups (`/admin/groups`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/admin/groups` | ✅ Yes | Admin | List or search groups (query param `term`) |
| POST | `/admin/groups` | ✅ Yes | Admin | Create new group |
| PUT | `/admin/groups/{groupId}` | ✅ Yes | Admin | Update group |
| DELETE | `/admin/groups/{groupId}` | ✅ Yes | Admin | Delete group |
| POST | `/admin/groups/{groupId}/members` | ✅ Yes | Admin | Add member to group |
| DELETE | `/admin/groups/{groupId}/members/{username}` | ✅ Yes | Admin | Remove member from group |

### Settings (`/settings`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/settings` | ✅ Yes | Admin | List all settings (encrypted values masked) |
| GET | `/settings/{key}` | ✅ Yes | Admin | Get single setting |
| GET | `/settings/category/{category}` | ✅ Yes | Admin | List settings by category |
| PUT | `/settings` | ✅ Yes | Admin | Create or update setting |
| DELETE | `/settings/{key}` | ✅ Yes | Admin | Delete setting |
| GET | `/settings/static` | ❌ No | Public | Get public app settings (branding + version) |

#### Update Setting (Admin)

```http
PUT /api/settings
Authorization: Bearer <token>
Content-Type: application/json

{
  "key": "App.DemoModeEnabled",
  "value": "true",
  "category": "Application",
  "description": "Enable demo mode with sample pages"
}

Response 200 OK
```

### Themes (`/themes`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/themes` | ✅ Yes | All | List available themes |
| POST | `/themes` | ✅ Yes | Admin, Editor | Create custom theme |
| PUT | `/themes/{themeId}` | ✅ Yes | Admin, Editor | Update custom theme |
| DELETE | `/themes/{themeId}` | ✅ Yes | Admin | Delete custom theme |

#### Get Available Themes

```http
GET /api/themes
Authorization: Bearer <token>

Response 200 OK:
[
  {
    "id": "system-white",
    "name": "White",
    "isCustom": false,
    "tokens": {
      "primary": "#0f62fe",
      "secondary": "#6f6f6f",
      ...
    }
  },
  {
    "id": "custom-corporate",
    "name": "Corporate Blue",
    "isCustom": true,
    "organizationId": null,
    "tokens": { ... }
  }
]
```

### Branding Assets (`/branding/assets`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/branding/assets/{id}` | ❌ No | Public | Fetch logo/favicon by asset id |
| POST | `/branding/assets/{assetType}` | ✅ Yes | Admin | Upload `logo` or `favicon` |
| DELETE | `/branding/assets/{assetType}` | ✅ Yes | Admin | Remove `logo` or `favicon` |

**Branding settings** (app name, footer text/links, logo/favicon URLs) are exposed via `GET /settings/static` and updated via `PUT /settings` with keys:
`Branding.AppName`, `Branding.FooterText`, `Branding.FooterLinkUrl`, `Branding.FooterLinkLabel`, `Branding.LogoAssetId`, `Branding.FaviconAssetId`.

### Power BI (`/powerbi`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/powerbi/workspaces` | ✅ Yes | Admin, Editor | List Power BI workspaces |
| GET | `/powerbi/workspaces/{workspaceId}/reports` | ✅ Yes | All | List reports in workspace |
| GET | `/powerbi/workspaces/{workspaceId}/dashboards` | ✅ Yes | All | List dashboards in workspace |
| GET | `/powerbi/workspaces/{workspaceId}/datasets` | ✅ Yes | All | List datasets in workspace |
| POST | `/powerbi/embed/report` | ✅ Yes | All* | Generate embed token for report |
| POST | `/powerbi/embed/dashboard` | ✅ Yes | All* | Generate embed token for dashboard |
| GET | `/powerbi/diagnostics` | ✅ Yes | Admin | Run diagnostics (optional workspace/report query params) |

*Access controlled by page authorization

#### Generate Report Embed Token

```http
POST /api/powerbi/embed/report
Authorization: Bearer <token>
Content-Type: application/json

{
  "resourceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "pageId": 42,
  "enableRLS": true,
  "rlsRoles": ["Sales_Team", "North_Region"]
}

Response 200 OK:
{
  "accessToken": "eyJhbGc...",
  "embedUrl": "https://app.powerbi.com/view...",
  "tokenId": "...",
  "expiration": "2025-02-06T11:30:00Z"
}
```

### Data Refresh (`/refreshes`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| POST | `/refreshes/datasets/{datasetId}/run` | ✅ Yes | Admin | Trigger manual refresh |
| GET | `/refreshes/datasets/{datasetId}/history` | ✅ Yes | Admin | Get refresh history |
| GET | `/refreshes/schedules` | ✅ Yes | Admin | List refresh schedules |
| POST | `/refreshes/schedules` | ✅ Yes | Admin | Create schedule |
| PUT | `/refreshes/schedules/{scheduleId}` | ✅ Yes | Admin | Update schedule |
| DELETE | `/refreshes/schedules/{scheduleId}` | ✅ Yes | Admin | Delete schedule |
| POST | `/refreshes/schedules/{scheduleId}/toggle` | ✅ Yes | Admin | Enable/disable schedule |

#### Trigger Manual Refresh

```http
POST /api/refreshes/datasets/3fa85f64-5717-4562-b3fc-2c963f66afa6/run
Authorization: Bearer <token>

Response 200 OK:
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "datasetId": "...",
  "workspaceId": "...",
  "status": "Queued",
  "requestedAtUtc": "2025-02-06T10:30:00Z"
}
```

### Audit Logs (`/audit`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/audit` | ✅ Yes | Admin | List audit logs (supports `skip`/`take`) |
| GET | `/audit/user/{username}` | ✅ Yes | Admin | Filter logs by username |
| GET | `/audit/resource/{resource}` | ✅ Yes | Admin | Filter logs by resource |

#### Query Audit Logs

```http
GET /api/audit?skip=0&take=100
Authorization: Bearer <token>

Response 200 OK:
{
  "total": 23,
  "logs": [
    {
      "id": 101,
      "action": "LOGIN",
      "resource": "User",
      "details": "Invalid password",
      "username": "john.doe",
      "ipAddress": "192.168.1.100",
      "success": false,
      "timestamp": "2025-02-06T10:30:00Z",
      "userAgent": "Mozilla/5.0"
    },
    ...
  ]
}
```

### Health & Status

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/health` | ❌ No | Liveness check (process up) |
| GET | `/ready` | ❌ No | Readiness check (LiteDB accessible) |
| GET | `/metrics` | ❌ No | Prometheus metrics |

## Error Handling

### HTTP Status Codes

| Code | Meaning | Example |
|------|---------|---------|
| 200 OK | Successful GET/PUT | User data retrieved |
| 201 Created | Resource created | New page created |
| 202 Accepted | Request accepted (async) | Refresh queued |
| 400 Bad Request | Invalid input | Missing required field |
| 401 Unauthorized | No/invalid token | Token expired |
| 403 Forbidden | Insufficient permissions | Non-admin accessing admin endpoint |
| 404 Not Found | Resource doesn't exist | Page ID doesn't match any page |
| 409 Conflict | Duplicate/constraint violation | Username already exists |
| 429 Too Many Requests | Rate limited | Too many login attempts |
| 500 Internal Server Error | Unexpected error | Database connection failed |

### Error Response Format

```json
{
  "message": "User not found",
  "code": "USER_NOT_FOUND",
  "details": "User ID 3fa85f64... does not exist",
  "traceId": "X-Correlation-ID from response header"
}
```

## Rate Limiting

The API enforces rate limiting to prevent abuse:

- **General endpoints**: 100 requests per minute (per IP)
- **Auth endpoints**: 5 requests per minute (per IP)

When rate limited, the API returns:

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 45

{
  "message": "Rate limit exceeded. Try again in 45 seconds."
}
```

## Pagination

Some list endpoints support pagination via `skip` and `take` query parameters (e.g., `/audit`, `/refreshes/datasets/{datasetId}/history`).

## Filtering

Filtering is endpoint-specific. For example, use `/audit/user/{username}` or `/audit/resource/{resource}` for audit logs and `/directory/users?query=...` for user search.

## Correlation IDs

All requests are tagged with a unique `X-Correlation-ID` for request tracing:

```http
Response Headers:
X-Correlation-ID: 550e8400-e29b-41d4-a716-446655440000
```

Use this ID to correlate logs across components.

---

## Related Documentation

- [ARCHITECTURE.md](ARCHITECTURE.md) - System design and data models
- [SECURITY.md](SECURITY.md) - Authentication and authorization
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - API error troubleshooting
