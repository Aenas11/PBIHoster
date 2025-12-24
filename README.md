# PBIHoster

A modern Power BI hosting solution with dynamic dashboard layouts, user authentication, and role-based access control. Built for organizations that need to securely host and manage Power BI reports with an "App owns the data" approach.

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Tech Stack](#tech-stack)
- [Release & Versioning](#release--versioning)
- [Getting Started](#getting-started)
  - [Quick Start with Docker](#quick-start-with-docker)
  - [Development Setup](#development-setup)
- [Configuration](#configuration)
- [Demo Mode & Sample Data](#demo-mode--sample-data)
- [Onboarding Walkthroughs](#onboarding-walkthroughs)
- [User Guide](#user-guide)
- [Security](#security)
- [Troubleshooting](#troubleshooting)

## Overview

PBIHoster (also known as ReportTree) provides a secure, customizable platform for hosting Power BI reports. It features:

- **Hierarchical Navigation**: Organize reports in a tree structure with unlimited nesting
- **Dynamic Dashboards**: Drag-and-drop layout system with customizable components
- **Role-Based Access**: Control who can view, edit, or manage content
- **Custom Themes**: Light/dark modes plus corporate branding support
- **Secure Authentication**: JWT-based authentication with comprehensive security features
- **Audit Logging**: Track all user actions and security events

## Key Features

### 🔐 Authentication & Security
- JWT token-based authentication
- Three user roles: **Admin**, **Editor**, and **Viewer**
- Password policy enforcement (complexity requirements)
- Account lockout after failed login attempts
- API rate limiting to prevent abuse
- Comprehensive audit logging
- Security headers and CORS protection

### 📊 Page & Content Management
- **Dynamic Page Tree**: Create unlimited nested pages and folders
- **Drag-and-Drop Layouts**: Configurable dashboard components
- **Role-Based Visibility**: Control page access by user roles and groups
- **Public/Private Pages**: Choose which pages require authentication
- **Edit Mode**: Manage navigation structure without accidentally clicking links

### 🎨 Themes & Customization
- Built-in Carbon Design System themes (White, Gray 10, Gray 90, Gray 100)
- Custom corporate themes with full color token control
- Organization-specific theme libraries
- Persistent theme selection across sessions

### 👥 User & Group Management
- User profile management
- Group-based permissions
- Password change with validation
- Email configuration

### ⚙️ Settings & Configuration
- Centralized settings management
- Category-based organization
- Automatic encryption for sensitive values
- Ready for Power BI integration (ClientId, TenantId, etc.)

## Tech Stack

- **Backend**: ASP.NET Core (.NET 10) Web API
- **Frontend**: Vue 3 (Composition API) + TypeScript + Vite
- **Database**: LiteDB (embedded NoSQL, no separate database server needed)
- **UI Framework**: Carbon Design System v11
- **Deployment**: Docker Compose with Caddy reverse proxy (automatic HTTPS)
- **Authentication**: JWT Bearer tokens

## Release & Versioning

- **Semantic Versioning**: The root `VERSION` file controls the app version. Update it before merging to `main`.
- **CI Artifacts**: The Docker publish workflow tags images as `v<VERSION>` and `latest`, aligning runtime artifacts with the version file.
- **Release Notes & Changelog**: See [`RELEASE_NOTES.md`](RELEASE_NOTES.md) for highlights and [`CHANGELOG.md`](CHANGELOG.md) for detailed history.

## Getting Started


### Quick Start with Docker

The fastest way to deploy PBIHoster in production is to use the official Docker image as referenced in the provided `docker-compose.yml`(see [docker-compose](/deployment/docker-compose.yml)).

#### Prerequisites
- Docker and Docker Compose installed
- A domain name pointing to your server (for HTTPS)
- Ports 80 and 443 open on your firewall

#### Steps

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd PBIHoster/deployment
   ```

2. **Configure environment**
   ```bash
   cp .env.example .env
   ```

3. **Generate a secure JWT key**
   ```bash
   openssl rand -base64 32
   ```
   Copy the output for the next step.

4. **Edit `.env` file**
   ```bash
   nano .env
   ```
   
   **Critical settings to change:**
   - `JWT_KEY`: Paste the key generated in step 3
   - `CORS_ORIGIN_1`: Your domain (e.g., `https://reports.company.com`)

5. **Update Caddyfile with your domain**
   ```bash
   nano Caddyfile
   ```
   Replace `your-domain.com` with your actual domain.

6. **Deploy using Docker Compose**
   ```bash
   docker-compose up -d
   ```
   This will pull and run the official image `ghcr.io/aenas11/pbihoster:main` as defined in `docker-compose.yml`. The backend, frontend, and database are all included in this image. Caddy will handle HTTPS and reverse proxy.

7. **Verify deployment**
   ```bash
   # Check containers are running
   docker-compose ps
   
   # View logs
   docker-compose logs -f pbihoster
   ```

8. **Access the application**
   - Navigate to `https://your-domain.com`
   - Register the first user account
   - Promote first user to Admin (see [First Time Setup](#first-time-setup))

### Development Setup

For local development without Docker.

#### Prerequisites
- .NET 10 SDK
- Node.js 18+ and npm
- Git

#### Steps

1. **Clone and setup**
   ```bash
   git clone <repository-url>
   cd PBIHoster
   ```

2. **Backend setup**
   ```bash
   cd ReportTree.Server
   dotnet restore
   dotnet build
   ```

3. **Frontend setup**
   ```bash
   cd ../reporttree.client
   npm install
   ```

4. **Run backend** (in one terminal)
   ```bash
   cd ReportTree.Server
   dotnet watch run
   ```
   Backend runs on `http://localhost:5001` (or check launchSettings.json)

5. **Run frontend** (in another terminal)
   ```bash
   cd reporttree.client
   npm run dev
   ```
   Frontend runs on `http://localhost:5173` with API proxy configured

6. **Access locally**
- Open `http://localhost:5173`
- API requests automatically proxy to backend

## Demo Mode & Sample Data

- Toggle **Demo Mode** in **Admin → Settings → Static Application Settings** to preload safe demo pages.
- Demo pages include links to the starter dataset (`/sample-data/sample-sales.csv`) and a static report preview (`/onboarding/sample-report.svg`) so you can explore layouts without tenant data.
- The root navigation exposes **Demo Overview** and **Sample Insights** when demo mode is on; remove or swap these once you connect your tenant.

## Onboarding Walkthroughs

Quick visual guides (served from `/onboarding` and available in the in-app Help page):

- **Create pages**: `/onboarding/create-pages.svg`
- **Assign roles**: `/onboarding/assign-roles.svg`
- **Configure themes**: `/onboarding/configure-themes.svg`
- **Sample report preview**: `/onboarding/sample-report.svg`

Visit `/help` inside the app for links and context.

## Configuration

### Environment Variables

All configuration can be set via environment variables in Docker or `appsettings.json` for development.

#### Essential Settings

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `JWT_KEY` | Secret key for signing JWT tokens (256-bit minimum) | - | ✅ Yes |
| `JWT_ISSUER` | Token issuer identifier | `ReportTree` | No |
| `JWT_EXPIRY_HOURS` | Token expiration time in hours | `8` | No |

> The API will refuse to start unless `JWT_KEY`, `POWERBI_TENANT_ID`, `POWERBI_CLIENT_ID`, and the appropriate Power BI credential (secret or certificate) are provided via environment variables or Key Vault.

#### Security Settings

| Variable | Description | Default |
|----------|-------------|---------|
| `PASSWORD_MIN_LENGTH` | Minimum password length | `8` |
| `PASSWORD_REQUIRE_UPPERCASE` | Require uppercase letter | `true` |
| `PASSWORD_REQUIRE_LOWERCASE` | Require lowercase letter | `true` |
| `PASSWORD_REQUIRE_DIGIT` | Require number | `true` |
| `PASSWORD_REQUIRE_SPECIAL` | Require special character | `true` |
| `PASSWORD_MAX_FAILED_ATTEMPTS` | Failed logins before lockout | `5` |
| `PASSWORD_LOCKOUT_MINUTES` | Lockout duration | `15` |

#### Rate Limiting

| Variable | Description | Default |
|----------|-------------|---------|
| `RATE_LIMIT_ENABLED` | Enable rate limiting | `true` |
| `RATE_LIMIT_GENERAL` | Max requests per period (general) | `100` |
| `RATE_LIMIT_GENERAL_PERIOD` | Time period for general limit | `1m` |
| `RATE_LIMIT_AUTH` | Max requests per period (auth endpoints) | `5` |
| `RATE_LIMIT_AUTH_PERIOD` | Time period for auth limit | `1m` |

#### CORS Settings

| Variable | Description | Example |
|----------|-------------|---------|
| `CORS_ORIGIN_1` | Allowed origin #1 | `https://reports.example.com` |
| `CORS_ORIGIN_2` | Allowed origin #2 | `https://app.example.com` |
| `CORS_ALLOW_CREDENTIALS` | Allow cookies/auth headers | `true` |

#### Power BI Configuration

| Variable | Description | Required |
|----------|-------------|----------|
| `POWERBI_TENANT_ID` | Azure AD Tenant ID | ✅ Yes |
| `POWERBI_CLIENT_ID` | Azure AD Client ID (App ID) | ✅ Yes |
| `POWERBI_CLIENT_SECRET` | Client Secret (if AuthType is ClientSecret) | Conditional |
| `POWERBI_AUTH_TYPE` | `ClientSecret` or `Certificate` | No (Default: ClientSecret) |
| `POWERBI_CERTIFICATE_THUMBPRINT` | Certificate Thumbprint (if AuthType is Certificate) | Conditional |
| `POWERBI_CERTIFICATE_PATH` | Path to .pfx file (if AuthType is Certificate) | Conditional |

#### Key Vault Integration

| Variable | Description | Required |
|----------|-------------|----------|
| `KEY_VAULT_URI` | Azure Key Vault URI to load secrets at startup | No (recommended) |

- Secrets are read directly from Key Vault when `KEY_VAULT_URI` is set (or `AZURE_KEY_VAULT_URI` as an alternative).
- Use secret names that mirror configuration keys (e.g., `Jwt--Key`, `PowerBI--ClientSecret`).

### Application Settings

Settings can be managed via the admin panel UI or directly in the database.

**Settings Categories:**
- **General**: Application-wide settings
- **Security**: Security-related configuration
- **PowerBI**: Power BI connection details (ClientId, TenantId, WorkspaceId)
- **Email**: Email server configuration
- **Authentication**: Auth provider settings

Sensitive settings (containing "key", "secret", "password") are automatically encrypted.

## User Guide

### First Time Setup

After deploying, you need to create an admin user:

1. **Register the first user** at `/login`
   - The first user registered in the system is automatically assigned the **Admin** role.
   - Subsequent users will be registered as **Viewers** by default.

2. **Log in** with your new admin account.
3. **Navigate to Admin Panel** to configure settings and manage users.

### Managing Pages

#### Creating Your First Page

1. **Enter Edit Mode**: Click "Edit Pages" at the bottom of the side menu
2. **Add Top-Level Page**: Click "New Top Level Page"
3. **Fill in Details**:
   - Title: e.g., "Sales Dashboard"
   - Icon: Choose from Carbon icons
   - Roles: Select which roles can access (Admin, Editor, Viewer)
4. **Save**: Click "Create Page"

#### Creating Subpages (Folders)

1. **Enter Edit Mode**
2. **Click on Parent Page** to edit it
3. **Add Child Page**: Click "Add Child Page" button in the modal
4. **Fill in Details** and save

#### Managing Access Control

For each page, you can configure:
- **Public Access**: Toggle to allow unauthenticated users
- **Role-Based Access**: Select roles that can view the page
- **User Access**: Add specific users by username
- **Group Access**: Add user groups for easier management

### Managing Themes

#### Switching Themes

1. Click the **theme icon** in the header (top-right)
2. Select from available themes:
   - **White**: Light theme with white background
   - **Gray 10**: Light theme with subtle gray
   - **Gray 90**: Dark theme
   - **Gray 100**: Darker theme
   - Custom corporate themes (if available)

#### Creating Custom Themes (Admin/Editor)

1. Navigate to **Admin Panel** → **Themes**
2. Click **"Create Custom Theme"**
3. Fill in:
   - **Theme Name**: Your theme name
   - **Organization ID**: (Optional) Leave blank for global theme
   - **Theme Tokens**: JSON with color definitions
4. Use the sample JSON as a starting point
5. **Save** to make it available

### Managing Users & Groups

#### Creating Users (Admin only)

1. Go to **Admin Panel** → **Users & Groups** tab
2. Click **"Add User"**
3. Enter username, password, and assign roles
4. Optionally add to groups
5. Save

#### Creating Groups (Admin only)

1. Go to **Admin Panel** → **Groups** section
2. Click **"Create Group"**
3. Enter group name and description
4. Add members by username
5. Save

Groups can then be assigned to pages for easier access control.

### User Profile

Users can manage their own profile:

1. Click **user icon** in header → **Profile**
2. **View** username, email, roles, and group memberships
3. **Update Email**: Change email address
4. **Change Password**: Update password (requires current password)

### Audit Logs (Admin)

View all user actions and security events:

1. Go to **Admin Panel** → **Audit Logs** tab
2. **Filter** by user, resource, or date range
3. **Review** login attempts, lockouts, and changes

## Security

### Security Features

PBIHoster includes enterprise-grade security:

- ✅ **Password Policy**: Enforced complexity requirements
- ✅ **Account Lockout**: Automatic lockout after failed attempts (brute force protection)
- ✅ **Rate Limiting**: Prevents API abuse and DoS attacks
- ✅ **CORS Protection**: Restricts cross-origin requests
- ✅ **Security Headers**: X-Frame-Options, CSP, XSS protection
- ✅ **JWT Authentication**: Secure token-based auth
- ✅ **Audit Logging**: Complete trail of all actions
- ✅ **HTTPS**: Automatic SSL via Caddy (in Docker deployment)
- ✅ **Encrypted Settings**: Sensitive configuration values encrypted at rest

### Best Practices

**Before going to production:**

- [ ] Change `JWT_KEY` to a strong random value (256+ bits)
- [ ] Configure `CORS_ORIGIN` to match your frontend domain
- [ ] Review password policy and adjust if needed
- [ ] Test rate limiting doesn't impact legitimate users
- [ ] Enable HTTPS (automatic with Docker/Caddy)
- [ ] Set up database backups (mount `/data` volume)
- [ ] Review audit logs regularly
- [ ] Document your security configuration

### Monitoring Security

**Key metrics to monitor:**
- Failed login attempts (spike = potential attack)
- Account lockouts (frequent = brute force or UX issue)
- Rate limit hits (429 responses = bot activity)
- Audit log patterns

**Check audit logs for:**
```sql
-- Recent failed logins
db.AuditLog.find({ Action: "LOGIN", Success: false })

-- Account lockouts
db.AuditLog.find({ Action: "ACCOUNT_LOCKED" })

-- Password changes
db.AuditLog.find({ Action: "CHANGE_PASSWORD" })
```

## Troubleshooting

### Common Issues

#### "Account is locked" Error

**Cause**: Too many failed login attempts

**Solution**:
1. Wait for the lockout period (default 15 minutes)
2. Or manually unlock: Access LiteDB and delete the record from `AccountLockout` collection

#### CORS Errors in Browser

**Symptom**: Console shows "CORS policy blocked" errors

**Solution**:
1. Ensure `CORS_ORIGIN_1` matches your frontend domain exactly (including `https://`)
2. Restart the application: `docker-compose restart pbihoster`
3. Clear browser cache

#### Rate Limiting (429 Errors)

**Symptom**: Legitimate users getting "Too Many Requests" errors

**Solution**:
1. Increase rate limits in `.env`:
   ```bash
   RATE_LIMIT_GENERAL=200
   RATE_LIMIT_GENERAL_PERIOD=1m
   ```
2. Restart: `docker-compose restart pbihoster`

#### JWT Token Validation Fails

**Symptom**: Users can't authenticate despite correct credentials

**Solution**:
1. Verify `JWT_KEY` hasn't changed (changing it invalidates all tokens)
2. Check `JWT_EXPIRY_HOURS` isn't too short
3. Verify server system clock is accurate

#### Database Issues

**Symptom**: Application can't start or errors mention LiteDB

**Solution**:
1. Check database file permissions: `/data/reporttree.db` must be writable
2. Verify volume mount in `docker-compose.yml`
3. Check disk space: `df -h`

#### Frontend Not Loading

**Symptom**: Blank page or 404 errors

**Solution**:
1. Verify backend built the frontend: Check `ReportTree.Server/wwwroot/` has files
2. Rebuild: `dotnet publish ReportTree.Server/ReportTree.Server.csproj`
3. Check Caddy logs: `docker-compose logs caddy`

### Getting Help

**Check logs:**
```bash
# Application logs
docker-compose logs -f pbihoster

# Caddy (web server) logs
docker-compose logs -f caddy

# All logs
docker-compose logs -f
```

**Verify containers:**
```bash
docker-compose ps
```

**Restart services:**
```bash
# Restart everything
docker-compose restart

# Restart specific service
docker-compose restart pbihoster
```

---

## Architecture Notes

### Data Storage

- **Database**: LiteDB file at `/data/reporttree.db` (embedded, no separate server)
- **Collections**: Users, Pages, Groups, Themes, AuditLogs, Settings, LoginAttempts
- **Backups**: Simply backup the `/data` directory

### API Structure

- **Base URL**: `/api/`
- **Auth Endpoints**: `/api/login`, `/api/register`
- **Protected Routes**: Require `Authorization: Bearer <token>` header
- **Admin Routes**: Require Admin role
- **Editor Routes**: Require Admin or Editor role

### Frontend Routes

- `/` - Home/Welcome page
- `/login` - Login page
- `/profile` - User profile
- `/admin` - Admin panel (Admin only)
- `/page/:id` - Dynamic page viewer

---

## License

[Your License Here]

## Contributing

[Your Contribution Guidelines Here]

## Support

For issues and questions:
- Check the [Troubleshooting](#troubleshooting) section
- Review logs with `docker-compose logs`
- Open an issue on GitHub
