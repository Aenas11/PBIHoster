# PBIHoster Deployment Guide

## Quick Start

### Prerequisites
- Docker and Docker Compose installed
- Domain name pointing to your server (for HTTPS)
- Port 80 and 443 open on your firewall

### Initial Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd deployment
   ```

2. **Configure environment variables**
   ```bash
   cp .env.example .env
   ```

3. **Generate secure JWT key**
   ```bash
   # Generate a secure 256-bit key
   openssl rand -base64 32
   ```

4. **Edit `.env` file**
   ```bash
   nano .env
   ```
   
   Update at minimum:
   - `JWT_KEY`: Paste the generated key from step 3
   - `CORS_ORIGIN_1`: Your frontend domain (e.g., https://reports.example.com)


5. **Update Caddyfile**
   Edit `Caddyfile` and replace `your-domain.com` with your actual domain.

6. **Configure Power BI Integration**
   See the new section below for full details. At minimum, set these in your `.env`:
   - `POWERBI_TENANT_ID`: Your Azure AD tenant ID
   - `POWERBI_CLIENT_ID`: Azure AD App (client) ID
   - `POWERBI_CLIENT_SECRET`: App secret (if using ClientSecret auth)
   - `POWERBI_AUTH_TYPE`: `ClientSecret` or `Certificate` (default: ClientSecret)
   - (Optional) `POWERBI_CERTIFICATE_THUMBPRINT`, `POWERBI_CERTIFICATE_PATH` for certificate auth

   These are required for backend-to-PowerBI authentication. See below for Azure setup.

7. **Deploy**
   ```bash
   docker-compose up -d
   ```

8. **Verify deployment**
   ```bash
   # Check containers are running
   docker-compose ps
   
   # Check logs
   docker-compose logs -f pbihoster
   ```
   - `GET /health` returns `200 OK` if the process is up.
   - `GET /ready` runs the LiteDB readiness check by calling `LiteDatabase.GetCollectionNames()`; failures usually point to file permissions, disk issues, or a DB file lock.

9. **Access application**
   - Navigate to `https://your-domain.com`
   - The first user to register will need to be promoted to Admin manually

## Security Configuration

## Power BI Integration

### About Azure Power BI Embedded

[Azure Power BI Embedded](https://azure.microsoft.com/en-us/products/power-bi-embedded) is a Microsoft Azure service that allows developers to embed fully interactive Power BI reports and dashboards into custom applications. PBIHoster leverages this service using the "App Owns Data" model, where the application authenticates as a service principal and manages access for end users.

**Key Points:**
- Power BI Embedded enables secure, scalable analytics in your app without requiring users to have Power BI licenses.
- The backend obtains Azure AD tokens and generates embed tokens for the frontend.
- All report and dashboard rendering is handled by the Power BI JavaScript client in the browser.

### Power BI Tenant & Workspace Configuration (Required)

For embedding to work in "App Owns Data" mode, your Power BI tenant and workspaces must be configured as follows:

1. **Enable Service Principal Access**: In the Power BI Admin Portal, under *Tenant settings*, enable "Allow service principals to use Power BI APIs" and grant access to the required security groups or service principal.
2. **Workspace Assignment**: Ensure the Azure AD app (service principal) is added as an *Admin* or *Member* to the target Power BI workspaces.
3. **Premium Capacity (Recommended)**: For production and large-scale embedding, assign workspaces to a Power BI Premium capacity (A SKUs or P SKUs) for better performance and licensing compliance.
4. **Publish Reports**: Upload your .pbix reports to the configured workspaces.

If these steps are not completed, embedding will fail with authorization errors or missing content.

For more details, see the official [Power BI Embedded documentation](https://learn.microsoft.com/en-us/power-bi/developer/embedded/embed-sample-for-your-organization) and [Azure Power BI Embedded product page](https://azure.microsoft.com/en-us/products/power-bi-embedded).

PBIHoster supports secure embedding of Power BI reports and dashboards using the "App Owns Data" model. The backend authenticates with Azure AD and generates embed tokens for the frontend.

### Azure AD App Registration (Required)
1. Register a new App in [Azure Portal > Azure Active Directory > App registrations](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps).
2. Note the **Application (client) ID** and **Directory (tenant) ID**.
3. Under **Certificates & secrets**, create a new client secret (or upload a certificate for certificate auth).
4. Under **API permissions**, add:
   - `Power BI Service` > `Dataset.Read.All`, `Report.Read.All`, `Workspace.Read.All`
   - Grant admin consent for these permissions.
5. In Power BI Admin Portal, enable **Service Principal** access for your tenant and workspaces.

### Environment Variables
Set these in your `.env` (see `.env.example`):
```env
POWERBI_TENANT_ID=your-tenant-id
POWERBI_CLIENT_ID=your-client-id
POWERBI_CLIENT_SECRET=your-client-secret
POWERBI_AUTH_TYPE=ClientSecret
# Optional for certificate auth:
POWERBI_CERTIFICATE_THUMBPRINT=
POWERBI_CERTIFICATE_PATH=
POWERBI_AUTHORITY_URL=https://login.microsoftonline.com/{0}/
POWERBI_RESOURCE_URL=https://analysis.windows.net/powerbi/api
POWERBI_API_URL=https://api.powerbi.com
```
These are mapped to ASP.NET config in `docker-compose.yml` as `PowerBI__*`.

### Docker Compose Mapping
`docker-compose.yml` automatically maps these variables to the backend container. No manual changes needed unless customizing.

### Frontend Usage
Once configured, editors can add Power BI components to pages. The backend handles all token generation and security.

### Troubleshooting Power BI Embedding
- **"Invalid credentials"**: Check all Azure AD values and client secret/certificate.
- **"Service principal not enabled"**: Enable in Power BI Admin Portal.
- **"Insufficient permissions"**: Ensure API permissions are granted and admin consented.
- **Token errors**: Check system time, secret expiry, and Docker environment variable mapping.
- **See logs**: `docker-compose logs -f pbihoster` for backend errors.


### Essential Security Settings

#### JWT Configuration (Required)
```bash
# CRITICAL: Must be changed for production
JWT_KEY=<your-secure-key-from-openssl-rand>
JWT_ISSUER=ReportTree
JWT_EXPIRY_HOURS=8
```

#### Password Policy (Recommended Defaults)
```bash
PASSWORD_MIN_LENGTH=8
PASSWORD_MAX_LENGTH=128
PASSWORD_REQUIRE_UPPERCASE=true
PASSWORD_REQUIRE_LOWERCASE=true
PASSWORD_REQUIRE_DIGIT=true
PASSWORD_REQUIRE_SPECIAL=true
PASSWORD_MAX_FAILED_ATTEMPTS=5
PASSWORD_LOCKOUT_MINUTES=15
```

#### Rate Limiting (Recommended Defaults)
```bash
RATE_LIMIT_ENABLED=true
RATE_LIMIT_GENERAL=100          # 100 requests per minute
RATE_LIMIT_GENERAL_PERIOD=1m
RATE_LIMIT_AUTH=5               # 5 login attempts per minute
RATE_LIMIT_AUTH_PERIOD=1m
```

#### CORS (Configure for your domain)
```bash
# Add your frontend URL(s)
CORS_ORIGIN_1=https://reports.example.com
CORS_ORIGIN_2=https://reports-staging.example.com
CORS_ALLOW_CREDENTIALS=true
```

### Security Features Enabled by Default

✅ **HTTPS/TLS**: Automatic via Caddy with Let's Encrypt  
✅ **Security Headers**: X-Frame-Options, X-Content-Type-Options, CSP, etc.  
✅ **Rate Limiting**: Prevents brute force attacks  
✅ **Account Lockout**: Automatic after failed login attempts  
✅ **Password Policy**: Strong password requirements  
✅ **Audit Logging**: All security events logged  
✅ **JWT Authentication**: Secure token-based auth  

See [SECURITY.md](../SECURITY.md) for comprehensive security documentation.

## Updating the Application

```bash
# Pull latest image
docker-compose pull pbihoster

# Restart with new image
docker-compose up -d pbihoster

# View logs
docker-compose logs -f pbihoster
```

## Backup and Restore

### Backup Database
```bash
# Database is stored in Docker volume
docker run --rm -v pbihoster_data:/data -v $(pwd):/backup \
  alpine tar czf /backup/reporttree-backup-$(date +%Y%m%d).tar.gz -C /data .
```

### Restore Database
```bash
# Stop application
docker-compose stop pbihoster

# Restore data
docker run --rm -v pbihoster_data:/data -v $(pwd):/backup \
  alpine sh -c "cd /data && tar xzf /backup/reporttree-backup-YYYYMMDD.tar.gz"

# Start application
docker-compose start pbihoster
```

## Troubleshooting

### Check Logs
```bash
# Application logs
docker-compose logs -f pbihoster

# Caddy logs
docker-compose logs -f caddy
```

### Container Not Starting
```bash
# Check container status
docker-compose ps

# View detailed logs
docker-compose logs pbihoster

# Common issues:
# - JWT_KEY not set or too short
# - Port 80/443 already in use
# - Domain not pointing to server
```

### HTTPS Not Working
```bash
# Verify Caddyfile domain matches your DNS
# Check Caddy logs for certificate errors
docker-compose logs caddy

# Ensure ports 80 and 443 are accessible
sudo netstat -tlnp | grep -E ':(80|443)'
```

### Password Policy Too Strict
```bash
# Adjust in .env file:
PASSWORD_MIN_LENGTH=6
PASSWORD_REQUIRE_SPECIAL=false

# Restart application
docker-compose restart pbihoster
```

### Rate Limiting Issues
```bash
# Increase limits in .env:
RATE_LIMIT_GENERAL=200
RATE_LIMIT_AUTH=10

# Or disable temporarily:
RATE_LIMIT_ENABLED=false

# Restart application
docker-compose restart pbihoster
```

### Account Locked
Users locked out after failed attempts can be unlocked by:
1. Waiting for lockout period (default 15 minutes)
2. Admin clearing lockout in database (advanced)

## Production Checklist

- [ ] Changed `JWT_KEY` to secure random value
- [ ] Configured `CORS_ORIGIN_1` with your domain
- [ ] Updated `Caddyfile` with your domain
- [ ] DNS pointing to server
- [ ] Ports 80 and 443 open
- [ ] Regular backups scheduled
- [ ] Monitoring/alerting configured
- [ ] Reviewed security settings in `SECURITY.md`
- [ ] Tested login and password change
- [ ] Tested rate limiting doesn't affect normal users

## Architecture

```
┌─────────────┐
│   Internet  │
└──────┬──────┘
       │
       ▼
┌─────────────────────────────────┐
│  Caddy (Port 80/443)            │
│  - HTTPS/TLS Termination        │
│  - Automatic Certificates       │
└──────┬──────────────────────────┘
       │ HTTP (Internal)
       ▼
┌─────────────────────────────────┐
│  PBIHoster (Port 8080)          │
│  - ASP.NET Core API             │
│  - Vue 3 SPA                    │
│  - LiteDB Database              │
└─────────────────────────────────┘
```

## Support

For security vulnerabilities, see [SECURITY.md](../SECURITY.md).

For general issues, create a GitHub issue with:
- Docker Compose version
- Relevant logs
- Steps to reproduce
