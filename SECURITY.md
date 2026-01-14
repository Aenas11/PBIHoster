# Security Implementation Guide

## Overview
PBIHoster implements comprehensive security features for production deployment. This document outlines all security measures and configuration options.

## 🔐 Security Features Implemented

### 1. Password Policy Enforcement
- **Minimum/Maximum Length**: Configurable password length requirements
- **Complexity Requirements**: 
  - Uppercase letters
  - Lowercase letters
  - Digits
  - Special characters
- **Account Lockout**: Automatic lockout after failed login attempts
- **Validation**: Password strength validated on registration and password change

**Configuration** (via environment variables):
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

### 2. Account Lockout Protection
- **Automatic Lockout**: Account locked after N failed login attempts
- **Time-based Unlock**: Automatically unlocks after configured duration
- **Login Attempt Tracking**: All login attempts logged with IP, timestamp, success/failure
- **Brute Force Prevention**: Rate limiting on auth endpoints prevents rapid-fire attacks

**How it works**:
1. Failed login attempt recorded with IP and timestamp
2. System checks recent attempts within lockout window
3. If attempts exceed threshold, account locked for configured minutes
4. Successful login clears lockout and attempt history
5. Audit log captures all lockout events

### 3. API Rate Limiting
- **General Endpoints**: 100 requests/minute (configurable)
- **Auth Endpoints**: 5 requests/minute (configurable)
- **IP-based Tracking**: Limits enforced per IP address
- **Automatic 429 Responses**: Rate-limited requests receive HTTP 429 status

**Configuration**:
```bash
RATE_LIMIT_ENABLED=true
RATE_LIMIT_GENERAL=100
RATE_LIMIT_GENERAL_PERIOD=1m
RATE_LIMIT_AUTH=5
RATE_LIMIT_AUTH_PERIOD=1m
```

### 4. CORS (Cross-Origin Resource Sharing)
- **Configurable Origins**: Whitelist specific frontend domains
- **Credential Support**: Allow cookies/auth headers for cross-origin requests
- **Flexible Methods/Headers**: Supports all HTTP methods and headers

**Configuration**:
```bash
CORS_ORIGIN_1=https://reports.example.com
CORS_ORIGIN_2=https://reports-staging.example.com
CORS_ALLOW_CREDENTIALS=true
```

### 5. Security Headers
All responses include security headers to prevent common attacks:

| Header | Value | Purpose |
|--------|-------|---------|
| `X-Frame-Options` | `DENY` | Prevents clickjacking |
| `X-Content-Type-Options` | `nosniff` | Prevents MIME sniffing |
| `X-XSS-Protection` | `1; mode=block` | Enables XSS filter |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Controls referrer info |
| `Permissions-Policy` | `geolocation=(), microphone=(), camera=()` | Restricts browser features |

### 6. JWT Token Security
- **Strong Signing Keys**: Minimum 256-bit keys required
- **Configurable Expiry**: Default 8 hours, adjustable
- **Role-based Claims**: Supports Admin, Editor, Viewer roles
- **Group Claims**: Includes user group memberships
- **Secure Storage**: Keys managed via environment variables

**Configuration**:
```bash
JWT_KEY=your-secure-256-bit-key-here
JWT_ISSUER=ReportTree
JWT_EXPIRY_HOURS=8
```

### 7. Reverse Proxy Support
- **Forwarded Headers**: Correctly handles X-Forwarded-For and X-Forwarded-Proto
- **Real IP Detection**: Rate limiting and audit logs use real client IP
- **Caddy Integration**: Pre-configured for Caddy reverse proxy with HTTPS

### 8. Audit Logging
- **Comprehensive Tracking**: All security events logged
- **Event Types**: Login attempts, password changes, lockouts, failed auth
- **Context Capture**: Username, IP, user agent, timestamp
- **Success/Failure**: Tracks both successful and failed operations

## 🚀 Deployment Configuration

### Docker Compose
The `docker-compose.yml` includes all security environment variables with sensible defaults. To customize:

1. Copy `.env.example` to `.env`:
   ```bash
   cp deployment/.env.example deployment/.env
   ```

2. Generate a secure JWT key:
   ```bash
   openssl rand -base64 32
   ```

3. Update `.env` with your values:
   ```bash
   JWT_KEY=<your-generated-key>
   CORS_ORIGIN_1=https://yourdomain.com
   ```

4. Deploy:
   ```bash
   cd deployment
   docker-compose up -d
   ```

### Environment Variables Reference

#### Required (Change in Production)
- `JWT_KEY`: **MUST** be changed from default. Generate with `openssl rand -base64 32`

#### Optional (Sensible Defaults Provided)
All other security settings have production-ready defaults but can be customized:
- Password policy settings
- Rate limiting thresholds
- CORS origins
- Token expiry

## 🔒 Best Practices

### Pre-Production Checklist
- [ ] **Change JWT_KEY** to a strong random value (256+ bits)
- [ ] **Configure CORS** origins to match your frontend domain(s)
- [ ] **Review password policy** and adjust for your requirements
- [ ] **Test rate limiting** doesn't impact legitimate users
- [ ] **Enable HTTPS** via Caddy (already configured)
- [ ] **Set up database backups** (mount `/data` volume)
- [ ] **Review audit logs** regularly for suspicious activity
- [ ] **Test account lockout** recovery process
- [ ] **Document** your security configuration for operations team

### Operational Security
1. **Monitor Audit Logs**: Review `AuditLog` collection regularly
2. **Watch Failed Logins**: Spike in failed attempts may indicate attack
3. **Rotate JWT Keys**: Periodically rotate signing keys (invalidates all tokens)
4. **Update Dependencies**: Keep Docker images and packages updated
5. **Backup Database**: Regular backups of LiteDB file in `/data` volume

### Runtime Secret Management
- **Environment-first**: All secrets (JWT signing key, Power BI credentials) are read from environment variables at startup. The app will refuse to boot if any are missing or using default/weak placeholders.
- **Azure Key Vault ready**: Set `KEY_VAULT_URI` (or `AZURE_KEY_VAULT_URI`) to load secrets directly from a vault via managed identity. Ensure secret names match configuration keys, e.g. `Jwt--Key`, `PowerBI--ClientSecret`.
- **Deployment safety**: Keep secrets out of `appsettings.json` and git. Validate deployments by checking container logs for `Missing or insecure secrets` errors before exposing endpoints.

### Key Rotation Procedure
1. **Prepare new secrets**: Generate a new 256-bit JWT key (`openssl rand -base64 32`) and, if applicable, a new Power BI client secret or certificate.
2. **Stage in Key Vault**: Add the new values as fresh versions in Key Vault (`KEY_VAULT_URI`) using the same secret names. Verify access with `az keyvault secret show --name Jwt--Key --vault-name <name>`.
3. **Update environment**: If not using Key Vault, update `JWT_KEY` / `POWERBI_CLIENT_SECRET` environment variables on the host or orchestration platform. Never check values into source control.
4. **Restart with validation**: Restart the `pbihoster` container or app service. Startup validation will fail fast if any value is missing, preventing partial deployments with mixed secrets.
5. **Invalidate old tokens**: After successful rollout, remove old secret versions and prompt users to re-authenticate.

### Power BI Service Principal Least Privilege
- **Scope to workspaces**: Grant the service principal access only to required workspaces (e.g., Contributor on specific workspaces). Avoid tenant-wide roles.
- **Limit API permissions**: In Azure AD, keep permissions minimal (`Dataset.Read.All`/`Report.Read.All` as required) and avoid broad admin consents unless explicitly needed.
- **Rotate client credentials**: Store `POWERBI_CLIENT_SECRET` or certificates in Key Vault, and rotate alongside JWT keys.
- **Audit regularly**: Review activity logs for the service principal and remove unused workspaces or roles. Validate that multi-tenant access is disabled unless intentionally configured.

### Automated Security Checks
- **CI enforcement**: The `Security Scans` workflow runs CodeQL SAST, gitleaks secret scanning, dependency review (PRs), and vulnerability audits for .NET (`dotnet list package --vulnerable`) and the Vue client (`npm audit --audit-level=high`).
- **Dependency alerts**: Fix or suppress findings in CI before merging. Keep lockfiles updated to capture patched transitive dependencies.

### Security Layers

```
┌─────────────────────────────────────────┐
│  Internet                               │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│  Caddy (Reverse Proxy)                  │
│  - HTTPS/TLS Termination                │
│  - Certificate Management               │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│  ASP.NET Core Application               │
│  - Security Headers                     │
│  - Rate Limiting                        │
│  - CORS                                 │
│  - JWT Authentication                   │
│  - Account Lockout                      │
│  - Password Policy                      │
│  - Audit Logging                        │
└─────────────────────────────────────────┘
```

## 📊 Security Monitoring

### Key Metrics to Monitor
1. **Failed Login Rate**: Sudden spikes indicate brute force attempts
2. **Account Lockouts**: High lockout rate may indicate attack or UX issues
3. **Rate Limit Hits**: Frequent 429 responses may indicate bot activity
4. **Token Expirations**: Unusual patterns may indicate session hijacking attempts

### Audit Log Queries
Check for suspicious activity:
- Recent failed logins: `db.AuditLog.find({ Action: "LOGIN", Success: false })`
- Account lockouts: `db.AuditLog.find({ Action: "ACCOUNT_LOCKED" })`
- Password changes: `db.AuditLog.find({ Action: "CHANGE_PASSWORD" })`

## 🛠️ Troubleshooting

### Account Locked
**Symptom**: User cannot login, receives "Account is locked" message
**Solution**: 
1. Check `AccountLockout` collection for user record
2. Wait for lockout period to expire, or
3. Manually remove lockout: `db.AccountLockout.delete({ Username: "user" })`

### Rate Limiting Issues
**Symptom**: Legitimate users receiving 429 errors
**Solution**:
1. Check current limits in environment variables
2. Increase `RATE_LIMIT_GENERAL` or `RATE_LIMIT_GENERAL_PERIOD`
3. Restart application: `docker-compose restart pbihoster`

### CORS Errors
**Symptom**: Frontend cannot make API requests, browser shows CORS error
**Solution**:
1. Add frontend origin to `CORS_ORIGIN_1` environment variable
2. Ensure origin includes protocol (https://)
3. Restart application

### JWT Validation Fails
**Symptom**: Valid tokens rejected, users cannot authenticate
**Solution**:
1. Verify `JWT_KEY` hasn't changed (changing key invalidates all tokens)
2. Check token expiry with `JWT_EXPIRY_HOURS`
3. Ensure system clock is correct

## 🔄 Future Enhancements

### Planned Features
- [ ] **Refresh Tokens**: Long-lived tokens for token renewal without re-login
- [ ] **Multi-Factor Authentication (MFA)**: TOTP-based 2FA
- [ ] **Session Management**: Track active sessions, force logout
- [ ] **IP Whitelisting**: Allow specific IPs to bypass rate limits
- [ ] **Security Events API**: Expose security events for SIEM integration
- [ ] **Password History**: Prevent password reuse
- [ ] **Email Notifications**: Alert users of security events

### Contributing Security Improvements
If you identify security vulnerabilities or have enhancement suggestions:
1. **Do not** create public GitHub issues for vulnerabilities
2. Email security concerns to: [your-security-email]
3. Include detailed description and reproduction steps
4. Allow reasonable time for patching before public disclosure

## 📚 References
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)
- [Rate Limiting Patterns](https://cloud.google.com/architecture/rate-limiting-strategies-techniques)
