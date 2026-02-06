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
POST /auth/register
Content-Type: application/json

{
  "username": "john.doe",
  "email": "john@example.com",
  "password": "SecurePass123!"
}

Response 200 OK:
{
  "accessToken": "eyJhbGc...",
  "expiresIn": 28800,
  "tokenType": "Bearer",
  "username": "john.doe"
}
```

#### 2. Login

```http
POST /auth/login
Content-Type: application/json

{
  "username": "john.doe",
  "password": "SecurePass123!"
}

Response 200 OK:
{
  "accessToken": "eyJhbGc...",
  "expiresIn": 28800,
  "tokenType": "Bearer"
}

Response 401 Unauthorized (invalid credentials):
{
  "message": "Invalid username or password"
}

Response 423 Locked (account lockout):
{
  "message": "Account is locked due to too many failed login attempts. Try again later."
}
```

#### 3. Logout

```http
POST /auth/logout
Authorization: Bearer <token>

Response 200 OK:
{
  "message": "Logout successful"
}
```

### Token Expiration & Refresh

Tokens expire after configured duration (default: 8 hours). No automatic refresh endpoint currently implemented—users must re-authenticate.

**Future Enhancement**: Token refresh endpoint planned for v0.4.

## Endpoints by Resource

### Authentication (`/auth`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/auth/login` | ❌ No | Login with username/password |
| POST | `/auth/register` | ❌ No | Register new user (first user becomes Admin) |
| POST | `/auth/logout` | ✅ Yes | Logout and invalidate session |

### Pages (`/pages`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/pages` | ✅ Yes | All | List accessible pages (tree structure) |
| GET | `/pages/{pageId}` | ✅ Yes | All | Get single page details with access check |
| POST | `/pages` | ✅ Yes | Admin, Editor | Create new top-level page |
| PUT | `/pages/{pageId}` | ✅ Yes | Admin, Editor | Update page (title, icon, roles, layout) |
| DELETE | `/pages/{pageId}` | ✅ Yes | Admin | Delete page (cascade to children) |
| POST | `/pages/{pageId}/children` | ✅ Yes | Admin, Editor | Create child page (subpage) |
| PUT | `/pages/{pageId}/move` | ✅ Yes | Admin | Move page to different parent |
| GET | `/pages/{pageId}/access` | ✅ Yes | Admin | Get detailed access control settings |
| PUT | `/pages/{pageId}/access` | ✅ Yes | Admin | Update access control (roles, users, groups) |

#### Get Pages (Accessible)

```http
GET /api/pages
Authorization: Bearer <token>

Response 200 OK:
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Sales Dashboard",
    "description": "Q4 sales metrics",
    "icon": "dashboard",
    "isPublic": false,
    "allowedRoles": ["Admin", "Editor", "Viewer"],
    "children": [
      {
        "id": "...",
        "title": "North Region",
        "children": []
      }
    ],
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
  "description": "Q4 sales metrics",
  "icon": "chart--column",
  "allowedRoles": ["Admin", "Editor", "Viewer"],
  "isPublic": false
}

Response 201 Created:
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Sales Dashboard",
  ...
}
```

### Users (`/users`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/users` | ✅ Yes | Admin | List all users with roles |
| GET | `/users/me` | ✅ Yes | All | Get current logged-in user profile |
| GET | `/users/{userId}` | ✅ Yes | Admin | Get user details |
| POST | `/users` | ✅ Yes | Admin | Create new user |
| PUT | `/users/{userId}` | ✅ Yes | Admin, Self | Update user (roles, groups) |
| DELETE | `/users/{userId}` | ✅ Yes | Admin | Delete user |
| PUT | `/users/{userId}/roles` | ✅ Yes | Admin | Update user roles |
| PUT | `/users/{userId}/password` | ✅ Yes | Admin | Reset user password |
| POST | `/users/{userId}/unlock` | ✅ Yes | Admin | Unlock locked account |
| PUT | `/users/me/profile` | ✅ Yes | All | Update own profile (email) |
| PUT | `/users/me/password` | ✅ Yes | All | Change own password |

#### Get Current User Profile

```http
GET /api/users/me
Authorization: Bearer <token>

Response 200 OK:
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "username": "john.doe",
  "email": "john@example.com",
  "roles": ["Admin", "Editor"],
  "groups": ["Engineering", "Leadership"],
  "favoritePageIds": ["...", "..."],
  "lastLoginAt": "2025-02-06T10:30:00Z"
}
```

#### Update Own Password

```http
PUT /api/users/me/password
Authorization: Bearer <token>
Content-Type: application/json

{
  "currentPassword": "OldPass123!",
  "newPassword": "NewPass456!"
}

Response 200 OK:
{
  "message": "Password changed successfully"
}

Response 400 Bad Request (weak password):
{
  "message": "Password does not meet complexity requirements"
}
```

### User Groups (`/groups`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/groups` | ✅ Yes | Admin | List all groups |
| POST | `/groups` | ✅ Yes | Admin | Create new group |
| PUT | `/groups/{groupId}` | ✅ Yes | Admin | Update group |
| DELETE | `/groups/{groupId}` | ✅ Yes | Admin | Delete group |
| GET | `/groups/{groupId}/members` | ✅ Yes | Admin | List group members |
| POST | `/groups/{groupId}/members` | ✅ Yes | Admin | Add member to group |
| DELETE | `/groups/{groupId}/members/{userId}` | ✅ Yes | Admin | Remove member from group |

### Admin Settings (`/admin/settings`, `/settings`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/admin/settings` | ✅ Yes | Admin | List all settings |
| GET | `/admin/settings/{key}` | ✅ Yes | Admin | Get single setting |
| PUT | `/admin/settings/{key}` | ✅ Yes | Admin | Update setting |
| GET | `/settings/app` | ✅ Yes | All | Get public app settings |

#### Update Setting (Admin)

```http
PUT /api/admin/settings/App.DemoModeEnabled
Authorization: Bearer <token>
Content-Type: application/json

{
  "value": "true"
}

Response 200 OK:
{
  "key": "App.DemoModeEnabled",
  "value": "true",
  "category": "General",
  "description": "Enable demo mode with sample pages",
  "encrypted": false
}
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

### Branding (`/branding`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/branding` | ❌ No | Public | Get current branding (public) |
| POST | `/branding/logo` | ✅ Yes | Admin | Upload logo |
| DELETE | `/branding/logo` | ✅ Yes | Admin | Delete logo |
| PUT | `/branding/settings` | ✅ Yes | Admin | Update branding (name, links) |

#### Get Branding (Public)

```http
GET /api/branding

Response 200 OK:
{
  "appName": "Company Analytics",
  "logoUrl": "https://example.com/logo.png",
  "favicon": "https://example.com/favicon.ico",
  "footerLinks": [
    { "label": "Privacy", "url": "https://..." },
    { "label": "Support", "url": "https://..." }
  ]
}
```

### Power BI (`/powerbi`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/powerbi/workspaces` | ✅ Yes | Admin, Editor | List Power BI workspaces |
| GET | `/powerbi/workspaces/{workspaceId}/reports` | ✅ Yes | All | List reports in workspace |
| GET | `/powerbi/workspaces/{workspaceId}/dashboards` | ✅ Yes | All | List dashboards in workspace |
| POST | `/powerbi/embed/report` | ✅ Yes | All* | Generate embed token for report |
| POST | `/powerbi/embed/dashboard` | ✅ Yes | All* | Generate embed token for dashboard |

*Access controlled by page authorization

#### Generate Report Embed Token

```http
POST /api/powerbi/embed/report
Authorization: Bearer <token>
Content-Type: application/json

{
  "reportId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "pageId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "enableRLS": true,
  "rlsRoles": ["Sales_Team", "North_Region"]
}

Response 200 OK:
{
  "token": "eyJhbGc...",
  "embedUrl": "https://app.powerbi.com/view...",
  "reportId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "expiresIn": 3600
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

Response 202 Accepted:
{
  "runId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Queued",
  "requestedAt": "2025-02-06T10:30:00Z"
}
```

### Audit Logs (`/audit`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/audit` | ✅ Yes | Admin | Query audit logs (with filters) |

#### Query Audit Logs

```http
GET /api/audit?action=LOGIN&success=false&days=7
Authorization: Bearer <token>

Response 200 OK:
{
  "total": 23,
  "logs": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "action": "LOGIN",
      "resource": "User",
      "resourceId": "...",
      "userId": "...",
      "username": "john.doe",
      "ipAddress": "192.168.1.100",
      "success": false,
      "failureReason": "Invalid password",
      "createdAt": "2025-02-06T10:30:00Z"
    },
    ...
  ]
}
```

### Directory (`/directory`)

| Method | Endpoint | Auth | Roles | Description |
|--------|----------|------|-------|-------------|
| GET | `/directory` | ✅ Yes | All | List directory (pages + users) |
| GET | `/directory/search` | ✅ Yes | All | Search directory |

#### Search Directory

```http
GET /api/directory/search?q=dashboard
Authorization: Bearer <token>

Response 200 OK:
{
  "pages": [
    {
      "id": "...",
      "title": "Sales Dashboard",
      "description": "Q4 metrics"
    }
  ],
  "users": [
    {
      "id": "...",
      "username": "dashboard.admin",
      "email": "admin@example.com"
    }
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

Endpoints returning lists support pagination via query parameters:

```
GET /api/audit?page=2&pageSize=50

Response:
{
  "total": 523,
  "page": 2,
  "pageSize": 50,
  "data": [ ... ]
}
```

## Filtering

Query parameters allow filtering of results:

```
GET /api/audit?action=LOGIN&success=false&days=7&username=john

Supported filters vary by endpoint. See endpoint documentation.
```

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
