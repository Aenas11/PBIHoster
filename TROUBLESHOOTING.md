# Operations & Troubleshooting Guide

## Overview

This guide covers operational tasks, monitoring, common issues, and resolution steps for running PBIHoster in production and development environments.

## Operational Tasks

### Daily Operations

#### Monitoring Health

```bash
# Check application health
curl https://your-domain.com/health

# Check readiness (database connectivity)
curl https://your-domain.com/ready

# View Prometheus metrics
curl https://your-domain.com/metrics
```

**Expected Responses**:
- `/health`: `200 OK` (always) - indicates process is running
- `/ready`: `200 OK` (normal) or `503 Service Unavailable` (database issue)
- `/metrics`: Prometheus text format with metrics

#### Reviewing Logs

```bash
# Docker logs
docker-compose logs -f pbihoster

# Last 100 lines
docker-compose logs --tail=100 pbihoster

# Since specific time
docker-compose logs --since 2025-02-06T10:00:00 pbihoster

# Kubernetes logs
kubectl logs -f deployment/pbihoster
```

**Log Format**: Structured JSON with `CorrelationId` for request tracing

```json
{
  "@t": "2025-02-06T10:30:45.1234567Z",
  "@m": "User login successful",
  "CorrelationId": "550e8400-e29b-41d4-a716-446655440000",
  "UserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "IpAddress": "192.168.1.100",
  "@l": "Information"
}
```

#### Checking Database Health

```bash
# Verify database file exists and is readable
ls -la /data/reporttree.db

# Check file size (should be > 50KB normally)
du -h /data/reporttree.db

# Check disk space
df -h /data

# Restart if database is locked
docker-compose restart pbihoster
```

### Weekly Tasks

#### Review Audit Logs

Monitor for security events:

```bash
# Failed logins (last 7 days) - indicates brute force attempts
curl -X GET 'https://your-domain.com/api/audit?action=LOGIN&success=false&days=7' \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Account lockouts
curl -X GET 'https://your-domain.com/api/audit?action=ACCOUNT_LOCKED&days=7' \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Password changes
curl -X GET 'https://your-domain.com/api/audit?action=CHANGE_PASSWORD&days=7' \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

**Alerting thresholds**:
- >5 failed logins from single IP in 15 minutes → Potential brute force
- >10 account lockouts in 1 hour → Possible attack or UX issue
- Any unauthorized access attempts → Investigate immediately

#### Backup Database

```bash
# Full backup with timestamp
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
docker cp pbihoster:/data/reporttree.db \
  ./backup/reporttree_$TIMESTAMP.db

# Compress backup
gzip ./backup/reporttree_$TIMESTAMP.db

# List recent backups
ls -lh ./backup/ | head -10
```

**Retention Policy**: Keep backups for at least 30 days

#### Monitor Metrics

Key metrics to track (from `/metrics` endpoint):

```
http_server_request_duration_seconds_bucket{endpoint="/api/pages"}
http_server_request_duration_seconds_bucket{endpoint="/api/auth/login"}
http_requests_total{status="500"}
dotnet_exceptions_total
```

**Dashboard Example (Grafana)**:
- Request latency (p50, p95, p99)
- Error rate (5xx responses)
- Disk usage
- Database file size

### Monthly Tasks

#### Rotate Credentials

```bash
# 1. Generate new JWT key
NEW_JWT_KEY=$(openssl rand -base64 32)
echo "New JWT_KEY: $NEW_JWT_KEY"

# 2. Update Key Vault or .env
# For Key Vault:
az keyvault secret set --vault-name $VAULT_NAME --name "Jwt--Key" --value "$NEW_JWT_KEY"

# 3. Restart application
docker-compose restart pbihoster

# 4. Force re-authentication
# All existing tokens become invalid after JWT key change
# Users will be logged out automatically

# 5. Verify deployment
curl https://your-domain.com/health
```

#### Audit User Accounts

```bash
# List all users
curl -X GET 'https://your-domain.com/api/users' \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Check for inactive accounts (no login in 90 days)
# Review users with excessive permissions
# Remove or reset locks on inactive accounts
```

#### Review Settings

```bash
# Check all security settings
curl -X GET 'https://your-domain.com/api/admin/settings?category=Security' \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Verify critical values:
# - PASSWORD_MAX_FAILED_ATTEMPTS
# - PASSWORD_LOCKOUT_MINUTES
# - RATE_LIMIT_AUTH (should be 5 or less)
# - CORS_ORIGIN_* (should match your domain exactly)
```

## Troubleshooting

### Application Won't Start

#### Symptom
```
docker-compose up
error: /data/reporttree.db is locked
```

#### Diagnosis
1. Check for running containers
   ```bash
   docker ps | grep pbihoster
   ```

2. Check for orphaned processes
   ```bash
   lsof /data/reporttree.db
   ```

#### Resolution
```bash
# Option 1: Restart containers
docker-compose down
docker-compose up -d

# Option 2: Force restart
docker-compose restart pbihoster

# Option 3: Remove and recreate (if database is corrupted)
docker-compose down
rm -f /data/reporttree.db
docker-compose up -d
# App will reinitialize database
```

---

### 429 Rate Limiting Errors

#### Symptom
```
HTTP/1.1 429 Too Many Requests
Retry-After: 45

{"message": "Rate limit exceeded. Try again in 45 seconds."}
```

#### Causes
1. **Legitimate traffic spike**: Too many concurrent users
2. **Bot activity**: Automated scraping or brute force attempt
3. **Tight rate limits**: Configuration too strict for use case

#### Diagnosis
```bash
# Check request patterns in audit logs
curl -X GET 'https://your-domain.com/api/audit?days=1' \
  -H "Authorization: Bearer $ADMIN_TOKEN" | grep "429"

# Identify IP addresses with high request rates
docker-compose logs pbihoster | grep "rate limit"
```

#### Resolution

**Option 1**: Increase rate limits (if legitimate traffic)
```bash
# Update .env
RATE_LIMIT_GENERAL=200          # Increase from 100
RATE_LIMIT_GENERAL_PERIOD=1m

RATE_LIMIT_AUTH=10              # Increase from 5
RATE_LIMIT_AUTH_PERIOD=1m

# Restart
docker-compose restart pbihoster
```

**Option 2**: Block malicious IP (if bot activity)
```bash
# Add firewall rule at Caddy level
# Edit Caddyfile:
yourdomain.com {
    @blocked {
        remote_ip 192.168.1.100
    }
    respond @blocked 403
    
    reverse_proxy localhost:8080
}

docker-compose restart caddy
```

**Option 3**: Implement IP whitelist for sensitive endpoints
```bash
# In Caddyfile:
yourdomain.com {
    @admin {
        path /api/admin/*
        not remote_ip 10.0.0.0/8    # Only allow internal IPs
    }
    respond @admin 403
    
    reverse_proxy localhost:8080
}
```

---

### CORS Errors

#### Symptom
```
Access to XMLHttpRequest at 'https://api.example.com/api/users'
from origin 'https://reports.example.com' has been blocked by CORS policy
```

#### Causes
1. **Missing CORS configuration**: `CORS_ORIGIN_*` not set
2. **Domain mismatch**: CORS origin doesn't match exactly
3. **Protocol mismatch**: http vs https
4. **Port mismatch**: localhost:5173 vs localhost:3000

#### Diagnosis
```bash
# Check current CORS configuration
docker-compose logs pbihoster | grep "CORS"

# Or query settings
curl -X GET 'https://your-domain.com/api/admin/settings?category=Security' \
  -H "Authorization: Bearer $ADMIN_TOKEN" | grep CORS
```

#### Resolution
```bash
# Update .env with correct origin(s)
CORS_ORIGIN_1=https://reports.example.com
CORS_ORIGIN_2=https://reports-staging.example.com
CORS_ALLOW_CREDENTIALS=true

# Restart
docker-compose restart pbihoster

# Test CORS preflight request
curl -i -X OPTIONS 'https://api.example.com/api/users' \
  -H "Origin: https://reports.example.com" \
  -H "Access-Control-Request-Method: GET"

# Should return:
# Access-Control-Allow-Origin: https://reports.example.com
# Access-Control-Allow-Methods: GET, POST, PUT, DELETE
```

---

### Account Locked - Can't Login

#### Symptom
```
Error: Account is locked due to too many failed login attempts.
Try again later.
```

#### Causes
1. **User forgot password**: Multiple failed attempts
2. **Brute force attack**: Attacker trying credentials
3. **Configuration issue**: Lockout period too long

#### Resolution (User Perspective)

1. Wait for lockout period to expire (default: 15 minutes)
2. Contact admin to unlock manually

#### Resolution (Admin)

```bash
# Check who is locked
curl -X GET 'https://your-domain.com/api/audit?action=ACCOUNT_LOCKED&days=1' \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Unlock a user
curl -X POST 'https://your-domain.com/api/users/{userId}/unlock' \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Or reset password
curl -X PUT 'https://your-domain.com/api/users/{userId}/password' \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"newPassword":"TempPassword123!"}'
```

#### Prevention

```bash
# Adjust lockout settings to be less aggressive
PASSWORD_MAX_FAILED_ATTEMPTS=10        # Increase from 5
PASSWORD_LOCKOUT_MINUTES=30            # Increase from 15

# Implement rate limiting on login endpoint (already enabled)
RATE_LIMIT_AUTH=10
RATE_LIMIT_AUTH_PERIOD=1m
```

---

### Database File Corruption

#### Symptom
```
LiteDB exception: Invalid BSON data
OR
Database is locked and cannot be accessed
OR
GET /ready returns 503 Service Unavailable
```

#### Diagnosis
```bash
# Check database file
file /data/reporttree.db
# Should output: LiteDB database

# Check file size (should be > 50KB)
ls -lh /data/reporttree.db

# Check if process has lock
lsof /data/reporttree.db

# Check container logs for specific error
docker-compose logs pbihoster | grep -i "litdb\|database\|error"
```

#### Resolution

**Option 1**: Restart application (if temporary lock)
```bash
docker-compose restart pbihoster
docker-compose logs -f pbihoster | grep "Database\|Started"
```

**Option 2**: Recover from backup
```bash
# Stop application
docker-compose down

# Restore backup
cp /backup/reporttree_20250205.db /data/reporttree.db

# Restart
docker-compose up -d

# Verify
curl https://your-domain.com/ready
```

**Option 3**: Reinitialize (if all backups are bad)
```bash
# WARNING: This will delete all data!
docker-compose down
rm -f /data/reporttree.db
docker-compose up -d
# Database will be recreated on startup
# Seed demo data if enabled
```

---

### High Memory Usage

#### Symptom
```
docker stats pbihoster
CONTAINER          MEM USAGE / LIMIT     MEM %
pbihoster          512 MB / 1 GB        51.2%
```

#### Causes
1. **Memory leak**: Long-running application
2. **Large dataset**: Many pages or reports loaded
3. **Inefficient queries**: N+1 problem or loading full collections
4. **Token caching**: Tokens not being cleared

#### Diagnosis
```bash
# Check memory trends
docker stats pbihoster --no-stream | tail -5

# Check for memory spikes in logs
docker-compose logs pbihoster | grep -i "memory\|gc"

# Profile the application (if using .NET diagnostics)
# dotnet-trace collect -p <pid>
```

#### Resolution

**Option 1**: Increase container memory limit
```yaml
# docker-compose.yml
services:
  pbihoster:
    mem_limit: 2g
    memswap_limit: 2g
```

**Option 2**: Restart container periodically
```bash
# Add restart policy
restart: unless-stopped

# Or manual daily restart
0 2 * * * docker-compose restart pbihoster
```

**Option 3**: Optimize queries (development)
- Use pagination in list endpoints
- Lazy-load child pages
- Cache frequently-accessed data

---

### Slow API Responses

#### Symptom
```
Request takes >3 seconds
GET /api/pages - 4.5s
POST /api/powerbi/embed/report - 6.2s
```

#### Causes
1. **Database performance**: Large collections, missing indexes
2. **Power BI API calls**: Slow token generation or refresh
3. **Network latency**: External service calls
4. **Resource contention**: CPU/memory limits

#### Diagnosis
```bash
# Check response times in logs
docker-compose logs pbihoster | grep "duration\|elapsed"

# Check database file size
du -h /data/reporttree.db

# Monitor resource usage
docker stats pbihoster

# Profile slow endpoint
curl -w "@curl-format.txt" -o /dev/null -s 'https://your-domain.com/api/pages' \
  -H "Authorization: Bearer $TOKEN"
```

#### Resolution

**Option 1**: Optimize queries
- Add database indexes
- Use pagination
- Lazy-load collections

**Option 2**: Cache frequently accessed data
```csharp
// Implement caching in service
private static readonly MemoryCache _cache = new MemoryCache(...);

public async Task<List<Page>> GetPagesAsync()
{
    const string cacheKey = "pages_all";
    if (_cache.TryGetValue(cacheKey, out List<Page> cached))
        return cached;
    
    var pages = await _repository.GetAllAsync();
    _cache.Set(cacheKey, pages, TimeSpan.FromMinutes(5));
    return pages;
}
```

**Option 3**: Increase resources
```yaml
# docker-compose.yml
services:
  pbihoster:
    cpus: '2'           # 2 CPU cores
    mem_limit: 2g
```

**Option 4**: Optimize Power BI calls
- Use token caching
- Batch API requests
- Implement async/await properly

---

### SSL/HTTPS Issues

#### Symptom
```
curl: (60) SSL certificate problem: certificate has expired
or
curl: (51) Peer's Certificate issuer is not recognized
```

#### Causes
1. **Expired certificate**: Let's Encrypt cert not renewed (Caddy should handle automatically)
2. **Domain mismatch**: Certificate for different domain
3. **Self-signed certificate**: In development

#### Diagnosis
```bash
# Check certificate expiry
curl -v https://your-domain.com 2>&1 | grep -i "expir\|valid"

# Or use openssl
openssl s_client -connect your-domain.com:443 | grep -A5 "Validity"

# Check Caddy logs
docker-compose logs caddy | grep -i "certificate\|tls\|error"
```

#### Resolution

**For Let's Encrypt (Production)**:
```bash
# Ensure Caddy is running and accessible on ports 80/443
docker-compose up -d

# Let Caddy handle renewal automatically (it does!)
# Just ensure ports are open and domain DNS is correct

# Force renew if needed
docker-compose exec caddy caddy reload
```

**For self-signed (Development)**:
```bash
# Generate self-signed cert
openssl req -x509 -newkey rsa:4096 -keyout key.pem -out cert.pem -days 365 -nodes

# Update Caddyfile.dev
yourdomain.local {
    tls ./cert.pem ./key.pem
    reverse_proxy localhost:8080
}

# Add to /etc/hosts
127.0.0.1 yourdomain.local

# Access via https://yourdomain.local (accept self-signed warning)
```

---

### Power BI Integration Issues

#### Symptom
```
"Invalid credentials" when embedding reports
or
"Service principal not enabled" error
or
"Insufficient permissions" for datasets
```

#### Causes
1. **Wrong credentials**: `POWERBI_CLIENT_SECRET` incorrect
2. **Service principal not enabled**: Not configured in Power BI tenant settings
3. **App not in workspace**: Service principal not added as member
4. **Permissions missing**: Required API permissions not granted

#### Diagnosis
```bash
# Check Power BI configuration
curl -X GET 'https://your-domain.com/api/admin/settings?category=PowerBI' \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Check logs for specific error
docker-compose logs pbihoster | grep -i "powerbi\|401\|403\|invalid"

# Verify Azure AD app credentials
az ad sp show --id <CLIENT_ID> --query "appOwnerOrganizationId"
```

#### Resolution

**1. Verify Azure AD app registration**
```bash
# Get your app
az ad app show --id <CLIENT_ID>

# Verify credentials
az ad app credential list --id <CLIENT_ID>

# Reset password if needed
az ad app credential reset --id <CLIENT_ID>
```

**2. Enable Service Principal in Power BI**
- Power BI Admin Portal
- Tenant Settings
- Search "Allow service principals"
- Enable for your security group
- Add your service principal to the group

**3. Add Service Principal to workspace**
- Power BI Workspace
- Workspace Settings
- Access
- Add your service principal as Admin/Member

**4. Grant API permissions**
- Azure Portal > App registrations > Your app
- API permissions
- Add permissions:
  - Power BI Service
  - User.Read.All
  - Report.Read.All
  - Dataset.Read.All
- Grant admin consent

**5. Test connection**
```bash
# Verify credentials and permissions
curl -X GET 'https://your-domain.com/api/powerbi/workspaces' \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Should return list of workspaces
```

---

## Performance Tuning

### Database Optimization

```bash
# Regular maintenance (once a month)
docker-compose exec pbihoster dotnet run -- --maintenance

# Or manually via Program.cs
var db = new LiteDatabase("reporttree.db");
db.Shrink();  // Defragment
```

### Caching Strategy

Implement caching for frequently accessed data:

```csharp
// Cache pages tree (expires every 5 minutes)
"pages_tree_cache": 300s

// Cache themes (expires every hour)
"themes_cache": 3600s

// Cache Power BI workspaces (expires every 10 minutes)
"powerbi_workspaces_cache": 600s

// Invalidate caches on update
_cache.Remove("pages_tree_cache");
```

### Connection Pooling

LiteDB uses file-based storage, no connection pooling needed. However:

```csharp
// Use singleton for LiteDatabase
services.AddSingleton<ILiteDatabase>(provider => 
    new LiteDatabase("filename=/data/reporttree.db")
);

// Access in services via dependency injection
public MyService(ILiteDatabase db) { ... }
```

---

## Monitoring Best Practices

### Key Metrics to Track

| Metric | Threshold | Alert Action |
|--------|-----------|--------------|
| `/ready` failures | Any >2 min | Page on-call, check database |
| 5xx error rate | >2% / 10 min | Check logs, investigate |
| Request latency p99 | >3s / 5 min | Scale resources, optimize |
| Failed logins | >10 / 15 min | Possible attack, block IP |
| Rate limit hits | >20 / hour | Check logs, adjust limits |
| Disk usage | >80% | Free space, archive logs |
| Memory usage | >85% | Restart container |

### Alerting Configuration

**Prometheus example**:
```yaml
groups:
  - name: pbihoster
    rules:
      - alert: PBIHosterDown
        expr: up{job="pbihoster"} == 0
        for: 2m
        action: page

      - alert: HighErrorRate
        expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.02
        for: 10m
        action: notify_slack

      - alert: SlowRequests
        expr: histogram_quantile(0.99, http_request_duration_seconds) > 3
        for: 5m
        action: notify_ops
```

---

## Disaster Recovery

### Backup & Restore

```bash
# Daily backup at 2 AM
0 2 * * * docker cp pbihoster:/data/reporttree.db /backup/reporttree-$(date +\%Y\%m\%d).db

# Keep 30 days of backups
find /backup -name "reporttree-*.db" -mtime +30 -delete

# Restore procedure
docker-compose down
cp /backup/reporttree-20250205.db /data/reporttree.db
docker-compose up -d
```

### Recovery Time Objectives (RTO) & Recovery Point Objectives (RPO)

| Scenario | RTO | RPO | Procedure |
|----------|-----|-----|-----------|
| Container crash | 1 min | 0 min | Auto-restart via policy |
| Database corruption | 5 min | <1 day | Restore from backup |
| Data center failure | 30 min | <1 hour | Failover to backup site |
| Total data loss | 2-4 hours | <1 day | Restore from encrypted backup |

---

## Related Documentation

- [SECURITY.md](SECURITY.md) - Security implementation and configuration
- [DEPLOYMENT.md](deployment/DEPLOYMENT.md) - Production deployment steps
- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture and design
