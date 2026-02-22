# PBIHoster

> A modern, secure Power BI hosting platform for embedding analytics into applications and portals. Built for ISVs, consultancies, and enterprises that need to manage Power BI reports with user authentication, role-based access control, and corporate branding.

**Current Version**: v0.4.0 | **License**: MIT | **Status**: Actively Maintained

[![CI/CD](https://github.com/Aenas11/PBIHoster/actions/workflows/docker-publish.yml/badge.svg)](https://github.com/Aenas11/PBIHoster/actions/workflows/docker-publish.yml)
[![Security Scans](https://github.com/aenas11/pbihoster/workflows/Security%20Scans/badge.svg)](https://github.com/aenas11/pbihoster/actions)

## Quick Links

📖 **[Full Documentation](#documentation)** | 🚀 **[Deployment Guide](deployment/DEPLOYMENT.md)** | 💻 **[Architecture](ARCHITECTURE.md)** | 🔒 **[Security](SECURITY.md)** | 🛣️ **[Roadmap](ROADMAP.md)**

---

## What is PBIHoster?

PBIHoster is an open-source platform for hosting and managing Power BI reports using the "App owns the data" embedding model. It provides:

- **Secure Multi-User Access**: JWT-based authentication with role-based access control
- **Dynamic Content Organization**: Hierarchical page tree with drag-and-drop dashboard layouts
- **Enterprise Branding**: Custom themes, logos, and white-labeling capabilities
- **Comprehensive Audit Trail**: Track all user actions and security events
- **Simple Deployment**: Docker Compose with automatic HTTPS via Caddy
- **Zero Operational Dependencies**: Embedded LiteDB database (no external servers)

## Table of Contents

- [Quick Start](#quick-start)
- [Key Features](#key-features)
- [Tech Stack](#tech-stack)
- [Documentation](#documentation)
- [Use Cases](#use-cases)
- [Getting Help](#getting-help)

## Quick Start

### Docker (Recommended for Production)

```bash
# 1. Clone repository
git clone https://github.com/aenas11/pbihoster.git
cd pbihoster/deployment

# 2. Setup configuration
cp .env.example .env
openssl rand -base64 32 > jwt_key.txt
# Edit .env and set JWT_KEY, CORS_ORIGIN_1, and Power BI credentials

# 3. Update domain in Caddyfile
nano Caddyfile  # Replace yourdomain.com with your domain

# 4. Deploy
docker-compose up -d

# 5. Access application
# Navigate to https://yourdomain.com and register the first user (auto-promoted to Admin)
```

See [DEPLOYMENT.md](deployment/DEPLOYMENT.md) for detailed production setup and Power BI configuration.

### Local Development

```bash
# Backend (requires .NET 10 SDK)
cd ReportTree.Server
dotnet watch run          # http://localhost:5001

# Frontend (requires Node.js 18+, in another terminal)
cd reporttree.client
npm install && npm run dev  # http://localhost:5173

# Access http://localhost:5173 (API requests proxy to backend)
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for full development setup.

---

## Key Features

### 🔐 Security & Authentication
- ✅ JWT-based authentication with account lockout protection
- ✅ Three user roles: **Admin** (full control), **Editor** (create/edit), **Viewer** (read-only)
- ✅ Password complexity enforcement
- ✅ API rate limiting (prevents brute force and DoS)
- ✅ Comprehensive audit logging (all user actions)
- ✅ CORS protection and security headers
- ✅ Support for external identity providers (OIDC/OAuth2) - [Planned](ROADMAP.md#phase-2-advanced-authentication-2-3-weeks---v050)

### 📊 Content Management
- ✅ Hierarchical page tree (unlimited nesting for organizing reports)
- ✅ Drag-and-drop layout system with components
- ✅ Role-based and group-based access control per page
- ✅ Public page support (no authentication required)
- ✅ Favorites and bookmarks
- ✅ Edit mode for managing structure without navigating

### 🎨 Customization & Branding
- ✅ Four built-in themes (White, Gray 10, Gray 90, Gray 100) from Carbon Design System
- ✅ Custom corporate themes with full color control
- ✅ Logo upload and favicon customization
- ✅ Custom footer links
- ✅ App name customization

### 📈 Power BI Integration
- ✅ Secure embedding with "App owns the data" model
- ✅ Row-Level Security (RLS) support with component-level configuration
- ✅ Dynamic workspace selection
- ✅ Report and dashboard embedding
- ✅ **Dataset Refresh Management** (Admin)
  - ✅ Scheduled refresh with cron expressions and time zone support
  - ✅ Manual refresh triggering with rate limiting
  - ✅ Refresh history and status tracking
  - ✅ Email and webhook notifications
  - ✅ Retry policy with exponential backoff
  - ✅ CSV export of refresh history

### 👥 User & Group Management
- ✅ User profile management and password change
- ✅ Admin user creation and role assignment
- ✅ Group-based permissions
- ✅ Account lockout and unlock

### 📋 Audit & Compliance
- ✅ Comprehensive audit logging
- ✅ Filtering by user or resource (API)
- ⏳ Export audit logs (planned)
- ✅ Security event tracking (failed logins, lockouts, etc.)

---

## Tech Stack

| Layer | Technology | Notes |
|-------|-----------|-------|
| **Backend** | ASP.NET Core (.NET 10) | Modern, high-performance web API |
| **Frontend** | Vue 3 + TypeScript + Vite | Reactive SPA with type safety |
| **Database** | LiteDB | Embedded NoSQL - no separate DB server |
| **UI Components** | Carbon Design System v11 | Enterprise-grade design system |
| **Authentication** | JWT Bearer Tokens | Stateless, scalable auth |
| **Deployment** | Docker + Docker Compose | Single container with all components |
| **Reverse Proxy** | Caddy | Automatic HTTPS with Let's Encrypt |

---

## Documentation

### User & Deployment
- 🚀 [**Deployment Guide**](deployment/DEPLOYMENT.md) - Production setup, Power BI configuration, security checklist
- � [**Email Setup Guide**](documentation/EMAIL_SETUP_GUIDE.md) - Configure SMTP for refresh notifications (Gmail, Office 365, SendGrid, etc.)
- �📖 [**User Guide**](README.md#user-guide) - Creating pages, managing users, configuring themes
- 🔒 [**Security Guide**](SECURITY.md) - Authentication, authorization, best practices
- 📋 [**Operations & Troubleshooting**](TROUBLESHOOTING.md) - Monitoring, common issues, recovery

### Developers
- 🏗️ [**Architecture**](ARCHITECTURE.md) - System design, layered architecture, data models
- 🔌 [**API Documentation**](API.md) - REST endpoints, authentication, error handling
- 🗄️ [**Database Schema**](DATABASE.md) - LiteDB collections, relationships, queries
- 🤝 [**Contributing**](CONTRIBUTING.md) - Development setup, code standards, PR process
- 🛣️ [**Roadmap**](ROADMAP.md) - Planned features, implementation timeline

### Reference
- 📝 [**Changelog**](CHANGELOG.md) - Detailed history of all releases
- 📢 [**Release Notes**](documentation/RELEASE_NOTES.md) - Highlights of latest release

---

## Use Cases

### ISVs & SaaS Products
Embed Power BI analytics directly into your application, whitelabeled with your branding. Users don't need Power BI licenses—your app manages authentication and access.

### Management Consultancies
Deliver custom analytics portals to clients with role-based access, audit trails, and automatic HTTPS. One instance per client for complete data isolation.

### Enterprise Analytics Teams
Host internal analytics portals with organizational hierarchies, group-based permissions, and comprehensive audit logging for compliance.

### Solution Architects
Create reusable analytics hosting infrastructure as a platform component, with templated deployments and standardized security practices.

---

## Installation Requirements

### For Docker Deployment (Production)
- Docker & Docker Compose
- A domain name with DNS pointing to your server
- Ports 80 and 443 open (HTTP/HTTPS)
- Azure AD app for Power BI integration

### For Local Development
- .NET 10 SDK
- Node.js 18+ with npm
- Git
- VS Code (optional)

---

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
## User Guide

### First Time Setup

1. **Access the application**
   - Navigate to your deployment URL
   - Click "Register" and create your first user account
   - The first user is automatically promoted to **Admin** role
   - Log in with your credentials

2. **Configure basic settings** (as Admin)
   - Navigate to **Admin Panel** → **Settings**
   - Set your organization's name, logo, and colors
   - Configure Power BI integration (if using reports)

3. **Create your first page** (optional)
   - Click "Edit Pages" in the sidebar
   - Add a new top-level page
   - Assign roles that can access it
   - Save and exit edit mode

### Managing Content

**Creating Pages & Hierarchy**
- Pages can be nested infinitely (folders → subfolders → pages)
- Each page can have a layout with draggable components
- Set access control per page (roles, users, groups, or public)
- See [ARCHITECTURE.md](ARCHITECTURE.md#data-model) for data model details

**Embedding Power BI Reports**
- Add "Power BI Report" components to page layouts
- Select workspace, report, and optionally configure RLS roles
- Reports display securely within your app

**Managing Users**
- Create users in Admin Panel → Users
- Assign roles (Admin, Editor, Viewer)
- Add users to groups for bulk access management
- Reset passwords or unlock accounts as needed

### Demo Mode & Sample Content

Toggle **Demo Mode** in Admin Panel → Settings to see:
- Sample pages and navigation structure
- Sample Power BI report preview (static)
- Sample dataset for reference

Useful for exploring without configuring Power BI first.

---

## Deployment

### Production Deployment (Recommended)

See the comprehensive [**Deployment Guide**](deployment/DEPLOYMENT.md) for:
- Step-by-step Docker Compose setup
- Power BI configuration and authentication
- Security hardening checklist
- HTTPS and reverse proxy setup
- Backup and recovery procedures

### Local Development

See [**Contributing Guide**](CONTRIBUTING.md) for development environment setup with hot-reload.

---

## Security & Compliance

### Security Features

- ✅ JWT-based authentication with automatic expiry
- ✅ Password complexity enforcement and account lockout
- ✅ Row-Level Security (RLS) for Power BI reports
- ✅ Role-based access control (Admin, Editor, Viewer)
- ✅ Group-based permissions for bulk access management
- ✅ API rate limiting (prevents brute force attacks)
- ✅ Comprehensive audit logging (all actions tracked)
- ✅ Security headers and CORS protection
- ✅ Encrypted credentials and sensitive data at rest
- ✅ Automatic HTTPS with Let's Encrypt (Docker)

### Pre-Production Checklist

See [**Security Guide**](SECURITY.md) for detailed security implementation and:
- [ ] Change `JWT_KEY` to a strong random value
- [ ] Configure `CORS_ORIGIN` for your domain(s)
- [ ] Set up database backups
- [ ] Review and adjust password policy
- [ ] Enable audit log monitoring
- [ ] Test account lockout recovery
- [ ] Verify Power BI service principal configuration

---

## Architecture & Technical Details

### System Architecture

PBIHoster follows a layered architecture:

```
Frontend (Vue 3 + TypeScript)
        ↓
API Layer (ASP.NET Core REST API)
        ↓
Service Layer (Business logic)
        ↓
Repository Layer (Data access with LiteDB)
        ↓
LiteDB (Embedded database)
```

See [**ARCHITECTURE.md**](ARCHITECTURE.md) for complete system design, data models, and integration patterns.

### REST API

All operations are available via REST API:

```bash
curl -X GET https://your-domain.com/api/pages \
  -H "Authorization: Bearer $TOKEN"
```

See [**API.md**](API.md) for complete endpoint documentation with examples.

### Database Schema

LiteDB collections and their relationships:

| Collection | Purpose |
|-----------|---------|
| `AppUser` | User accounts, authentication |
| `Page` | Page hierarchy, layouts, access control |
| `AppSetting` | Configuration (encrypted for sensitive data) |
| `AuditLog` | Comprehensive audit trail |
| `Group` | User groups for bulk access management |
| `CustomTheme` | Custom branding and color tokens |
| `LoginAttempt` | Failed login tracking (lockout) |
| `DatasetRefreshSchedule` | Scheduled Power BI dataset refreshes |
| `DatasetRefreshRun` | Refresh execution history |

See [**DATABASE.md**](DATABASE.md) for complete schema documentation and query examples.

---

## Support & Community

### Getting Help

- 📖 **[Full Documentation](#documentation)** - Guides for all topics
- 🐛 **[GitHub Issues](https://github.com/aenas11/pbihoster/issues)** - Bug reports and feature requests
- 💬 **[GitHub Discussions](https://github.com/aenas11/pbihoster/discussions)** - Ask questions, share ideas
- 🛠️ **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** - Common issues and resolutions

### Contributing

We welcome contributions! See [**CONTRIBUTING.md**](CONTRIBUTING.md) for:
- Development setup guide
- Code standards and conventions
- Pull request process
- Testing requirements

### Reporting Issues

When reporting bugs, please include:
- Your environment (Docker, local, Kubernetes, etc.)
- Application version (from `/version` endpoint)
- Steps to reproduce
- Expected vs. actual behavior
- Relevant logs (from `/api/audit` or container logs)

---

## Versioning & Releases

**Semantic Versioning**: Major.Minor.Patch (e.g., 0.3.0)

- **CHANGELOG.md**: Detailed history of all releases
- **documentation/RELEASE_NOTES.md**: Highlights of latest release
- **ROADMAP.md**: Planned features and timeline

---

## License

PBIHoster is released under the **MIT License** - see [LICENSE](LICENSE) file for details.

You are free to:
- ✅ Use commercially
- ✅ Modify the source code
- ✅ Distribute and sublicense
- ✅ Use privately

---

## Acknowledgments

- **Microsoft Power BI** for the embedded analytics platform
- **Carbon Design System** for the enterprise UI framework
- **LiteDB** for the embedded database
- **Caddy** for the automated reverse proxy

---

## Related Resources

### Microsoft Power BI
- [Power BI Embedded Documentation](https://learn.microsoft.com/en-us/power-bi/developer/embedded/)
- [Power BI REST API Reference](https://learn.microsoft.com/en-us/rest/api/power-bi/)
- [App owns the data sample](https://github.com/Microsoft/PowerBI-Developer-Samples)

### Design System
- [Carbon Design System](https://www.carbondesignsystem.com/)
- [Carbon Vue Components](https://carbon-components-vue.netlify.app/)
- [Carbon Icons](https://www.carbondesignsystem.com/elements/icons/library/)

### Technologies
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/dotnet/core/aspnet/)
- [Vue 3 Guide](https://vuejs.org/)
- [LiteDB Documentation](https://docs.litedb.org/)
- [Docker Documentation](https://docs.docker.com/)

---

## Contact & Links

- **GitHub**: [aenas11/pbihoster](https://github.com/aenas11/pbihoster)
- **Issues**: [Report a bug](https://github.com/aenas11/pbihoster/issues)
- **Discussions**: [Ask a question](https://github.com/aenas11/pbihoster/discussions)

---

**Last Updated**: 2025-02-06 | **Version**: 0.3.0

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
