# Database Schema Documentation

## Overview

PBIHoster uses **LiteDB**, an embedded NoSQL database. All data is stored in a single file (`/data/reporttree.db`) with no external database server required.

**Location**: `/data/reporttree.db` (in Docker: mounted volume)  
**ORM/Access**: Direct LiteDB API via Repository Pattern  
**Persistence**: `ReportTree.Server/Persistance/`

## Collections & Entities

### AppUser

Stores user account information, authentication, and profile data.

```csharp
{
  "_id": ObjectId,
  "Id": Guid,
  "Username": string (unique),
  "Email": string,
  "PasswordHash": string (nullable for external users),
  "Roles": List<string>,           // ["Admin", "Editor", "Viewer"]
  "FavoritePageIds": List<Guid>,
  "AuthProvider": string,           // "Local", "AzureAd", "Okta", etc.
  "ExternalUserId": string,
  "CreatedAt": DateTime,
  "UpdatedAt": DateTime,
  "LastLoginAt": DateTime (nullable),
  "IsLocked": bool,
  "LockedUntil": DateTime (nullable)
}
```

**Indexes**:
- Username (unique)
- Email
- AuthProvider + ExternalUserId (unique when both non-null)

**Usage**:
- User authentication and profile management
- Favorite pages tracking
- Role assignment and permission checks

---

### Page

Represents pages/content in the navigation tree. Supports unlimited nesting via parent-child relationships.

```csharp
{
  "_id": ObjectId,
  "Id": Guid (unique),
  "Title": string,
  "Description": string (nullable),
  "Icon": string (nullable),              // Carbon Design icon name
  "ParentPageId": Guid (nullable),        // null for root pages
  "ChildPageIds": List<Guid>,
  "AllowedRoles": List<string>,           // Roles with access
  "IsPublic": bool,                       // No auth required
  "Layout": PageLayout (nullable),        // Drag-drop layout config
  "CreatedAt": DateTime,
  "UpdatedAt": DateTime,
  "CreatedByUserId": Guid,
  "UpdatedByUserId": Guid
}
```

**PageLayout Structure**:
```csharp
{
  "Components": List<LayoutComponent>
}
```

**LayoutComponent Structure**:
```csharp
{
  "Id": string,                           // Unique within page
  "Type": string,                         // "PowerBIReport", "PowerBIDashboard", "Text", "Image", etc.
  "Position": GridPosition,
  "Config": Dictionary<string, object>    // Type-specific config
}
```

**Indexes**:
- Id (unique)
- ParentPageId (for tree traversal)
- CreatedAt (for sorting)

**Usage**:
- Navigation tree structure
- Page-level access control
- Dashboard layout storage
- Content organization

**Queries**:
```csharp
// Get all root pages (access control applied in service layer)
var rootPages = _pageRepository.GetByParentId(null);

// Get children of a page
var children = _pageRepository.GetByParentId(parentId);

// Get full page with access check
var page = _pageRepository.GetById(pageId);
```

---

### AppSetting

Stores application configuration and settings that can be managed via the admin UI.

```csharp
{
  "_id": ObjectId,
  "Key": string (unique),
  "Value": string,                        // May be encrypted if sensitive
  "Category": string,                     // "General", "Security", "PowerBI", "Email"
  "Description": string,
  "Encrypted": bool,
  "UpdatedAt": DateTime,
  "UpdatedByUserId": Guid
}
```

**Standard Settings**:
- `App.DemoModeEnabled` → Show demo pages
- `App.CompanyName` → Application name
- `Email.SmtpServer` → Email configuration

**Power BI Settings**: Stored in environment variables (not in AppSetting), e.g. `PowerBI__TenantId`, `PowerBI__ClientId`, `PowerBI__ClientSecret`.

**Encryption**: Settings with keys containing "key", "secret", "password" are automatically encrypted.

**Indexes**:
- Key (unique)
- Category

**Usage**:
- Centralized configuration management
- Feature toggles (demo mode, etc.)
- Sensitive credential storage (encrypted)

**Queries**:
```csharp
// Get all settings
var settings = _settingRepository.GetAll();

// Get by category
var powerBiSettings = _settingRepository.GetByCategory("PowerBI");

// Get single setting
var demoEnabled = _settingRepository.GetByKey("App.DemoModeEnabled");
```

---

### AuditLog

Comprehensive log of all security-relevant user actions.

```csharp
{
  "_id": ObjectId,
  "Id": Guid,
  "Action": string,                       // "LOGIN", "CREATE_PAGE", "DELETE_USER", "CHANGE_PASSWORD", etc.
  "Resource": string,                     // "User", "Page", "Settings", "AuthToken", etc.
  "ResourceId": string (nullable),
  "UserId": Guid (nullable),              // null for unauthenticated actions
  "Username": string,
  "IpAddress": string,
  "UserAgent": string (nullable),
  "Success": bool,
  "FailureReason": string (nullable),
  "Details": Dictionary<string, object>,  // Additional context
  "CreatedAt": DateTime
}
```

**Common Actions**:
- Authentication: LOGIN, LOGOUT, REGISTER, PASSWORD_RESET, ACCOUNT_LOCKED
- Authorization: ACCESS_DENIED, INSUFFICIENT_PERMISSIONS
- Content: CREATE_PAGE, UPDATE_PAGE, DELETE_PAGE, PUBLISH_PAGE
- User Management: CREATE_USER, DELETE_USER, UPDATE_ROLES, RESET_PASSWORD
- Security: CHANGE_PASSWORD, RATE_LIMIT_HIT, SUSPICIOUS_ACTIVITY

**Indexes**:
- CreatedAt (for time-range queries)
- Action (for filtering)
- UserId (for user activity)

**Usage**:
- Security event tracking
- Compliance and audit trails
- Intrusion detection (spike in failed logins)
- User activity reporting

**Queries**:
```csharp
// Failed logins in last 7 days
var recentFailures = _auditRepository.GetByFilter(
  action: "LOGIN",
  success: false,
  daysBack: 7
);

// User activity
var userActions = _auditRepository.GetByUserId(userId);

// Suspicious activity (multiple failed logins from same IP)
var suspiciousIps = _auditRepository.GetFailedLoginsByIp(
  minutes: 15,
  attemptThreshold: 5
);
```

---

### Group

User groups for organizing users and simplifying access control.

```csharp
{
  "_id": ObjectId,
  "Id": Guid (unique),
  "Name": string (unique),
  "Description": string (nullable),
  "MemberIds": List<Guid>,                // User IDs in this group
  "CreatedAt": DateTime,
  "UpdatedAt": DateTime,
  "CreatedByUserId": Guid
}
```

**Indexes**:
- Id (unique)
- Name (unique)

**Usage**:
- Group-based access control for pages
- Simplified user management
- Team/department organization

---

### BrandingAsset

Stores uploaded branding assets (logos, favicons).

```csharp
{
  "_id": ObjectId,
  "Id": Guid,
  "AssetType": string,                    // "Logo", "Favicon", "Banner"
  "FileName": string,
  "ContentType": string,                  // "image/png", "image/svg+xml", etc.
  "Data": byte[],                         // Binary file content
  "CreatedAt": DateTime,
  "CreatedByUserId": Guid
}
```

**File Storage**: Binary data stored directly in LiteDB  
**Serving**: Retrieved and served via `/api/branding/logo` endpoint

---

### CustomTheme

Custom corporate themes with color token configuration.

```csharp
{
  "_id": ObjectId,
  "Id": Guid,
  "Name": string,
  "Description": string (nullable),
  "IsSystemTheme": bool,
  "OrganizationId": Guid (nullable),      // For multi-tenant in future
  "Tokens": Dictionary<string, string>,   // { "primary": "#0f62fe", "secondary": "..." }
  "CreatedAt": DateTime,
  "UpdatedAt": DateTime,
  "CreatedByUserId": Guid
}
```

**Token Categories**:
- **Primary Colors**: primary, secondary, interactive, danger, warning, success
- **Backgrounds**: uiBackground, uiLayer, background, backgroundInverse
- **Borders**: borderSubtle, borderStrong
- **Text**: textPrimary, textSecondary, textOnColor, textInverse
- **Interactive**: hoverPrimary, activePrimary, focusInset, focusInverse
- **Etc**: skeletonBackground (and 90+ total tokens from Carbon Design System)

---

### LoginAttempt

Tracks login attempts for brute force protection.

```csharp
{
  "_id": ObjectId,
  "Id": Guid,
  "Username": string,
  "IpAddress": string,
  "Success": bool,
  "Timestamp": DateTime,
  "UserAgent": string (nullable),
  "FailureReason": string (nullable)
}
```

**Cleanup**: Old entries pruned automatically (retention: 30 days)  
**Usage**: Account lockout calculations, brute force detection

**Query Example**:
```csharp
// Check if account should be locked
var recentFailures = _db.GetCollection<LoginAttempt>()
  .Find(x => 
    x.Username == username && 
    !x.Success && 
    x.Timestamp > DateTime.UtcNow.AddMinutes(-15)
  )
  .Count();

if (recentFailures >= 5)
  LockAccount(username);
```

---

### DatasetRefreshSchedule

Scheduled dataset refresh configuration for Power BI datasets.

```csharp
{
  "_id": ObjectId,
  "Id": Guid,
  "Name": string,
  "DatasetId": Guid,
  "PageId": Guid (nullable),              // Associated page
  "ReportId": Guid (nullable),
  "Enabled": bool,
  "Cron": string,                         // "0 9 * * MON-FRI" (5 AM Mon-Fri)
  "TimeZone": string,                     // "America/New_York"
  "RetryCount": int,                      // 0-5
  "RetryBackoffSeconds": int,             // 30-3600
  "NotifyOnSuccess": bool,
  "NotifyOnFailure": bool,
  "NotifyTargets": List<string>,          // Email addresses or webhook URLs
  "CreatedAt": DateTime,
  "UpdatedAt": DateTime,
  "CreatedByUserId": Guid,
  "LastRunAt": DateTime (nullable)
}
```

**Cron Format**: Quartz cron (Day, Month, Day Of Week, etc.)  
**Timezones**: IANA timezone identifiers (e.g., "America/New_York")

---

### DatasetRefreshRun

Records of individual refresh executions.

```csharp
{
  "_id": ObjectId,
  "Id": Guid,
  "ScheduleId": Guid (nullable),          // null for manual refreshes
  "DatasetId": Guid,
  "TriggeredByUserId": Guid (nullable),   // null for system/scheduled
  "RequestedAtUtc": DateTime,
  "StartedAtUtc": DateTime (nullable),
  "CompletedAtUtc": DateTime (nullable),
  "Status": string,                       // "Queued", "InProgress", "Succeeded", "Failed", "Cancelled"
  "FailureReason": string (nullable),
  "PowerBiRequestId": string,             // For tracking with Microsoft
  "PowerBiActivityId": string,
  "RetriesAttempted": int,
  "DurationMs": int (nullable)
}
```

**Status Transitions**:
```
Queued → InProgress → Succeeded
              ↓
            Failed → Queued (retry)
                       ↓
                     Failed (max retries)
```

---

## Database Access Patterns

### Repository Pattern

Each entity type has a dedicated repository interface and implementation:

```csharp
// Interfaces (IUserRepository.cs, IPageRepository.cs, etc.)
public interface IUserRepository
{
    AppUser GetById(Guid id);
    AppUser GetByUsername(string username);
    List<AppUser> GetAll();
    Guid Create(AppUser user);
    void Update(AppUser user);
    void Delete(Guid id);
}

// Implementation (LiteDbUserRepository.cs, etc.)
public class LiteDbUserRepository : IUserRepository
{
    private readonly ILiteDatabase _db;
    
    public LiteDbUserRepository(ILiteDatabase db)
    {
        _db = db;
    }
    
    public AppUser GetById(Guid id)
    {
        return _db.GetCollection<AppUser>()
            .FindById(id);
    }
    
    // ... other methods
}
```

### Service Layer Usage

Services use repositories and add business logic:

```csharp
public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditRepository;
    
    public async Task<AuthResponse> LoginAsync(string username, string password, string ipAddress)
    {
        // 1. Get user
        var user = _userRepository.GetByUsername(username);
        if (user == null)
        {
            // 2. Log failed attempt
            _auditRepository.Create(new AuditLog {
                Action = "LOGIN",
                Username = username,
                Success = false,
                IpAddress = ipAddress,
                FailureReason = "User not found"
            });
            throw new UnauthorizedException("Invalid username or password");
        }
        
        // 3. Check account lockout
        if (user.IsLocked && user.LockedUntil > DateTime.UtcNow)
            throw new AccountLockedException("Account is locked");
        
        // 4. Validate password
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            // Record failed attempt
            RecordFailedLoginAttempt(user, ipAddress);
            throw new UnauthorizedException("Invalid username or password");
        }
        
        // 5. Login successful
        user.LastLoginAt = DateTime.UtcNow;
        user.IsLocked = false;  // Clear lock
        _userRepository.Update(user);
        
        // 6. Log successful login
        _auditRepository.Create(new AuditLog {
            Action = "LOGIN",
            UserId = user.Id,
            Success = true,
            IpAddress = ipAddress
        });
        
        // 7. Generate token
        var token = _tokenService.GenerateJwt(user);
        
        return new AuthResponse { AccessToken = token };
    }
}
```

## Initialization & Migrations

### Database Initialization

LiteDB database is automatically initialized on application startup:

```csharp
// Program.cs
var db = new LiteDatabase("filename=/data/reporttree.db");

// Create collections and indexes
var usersCollection = db.GetCollection<AppUser>();
usersCollection.EnsureIndex(x => x.Username);
usersCollection.EnsureIndex(x => x.Email);

var pagesCollection = db.GetCollection<Page>();
pagesCollection.EnsureIndex(x => x.ParentPageId);
```

### Seed Data (Demo Mode)

When demo mode is enabled, sample pages and data are created:

```csharp
// Services/DemoDataService.cs
public static void SeedDemoData(ILiteDatabase db)
{
    var pages = db.GetCollection<Page>();
    
    pages.Insert(new Page
    {
        Id = Guid.NewGuid(),
        Title = "Demo Overview",
        Description = "Demo pages and sample content",
        IsPublic = true,
        AllowedRoles = new List<string> { "Admin", "Editor", "Viewer" }
    });
    
    // ... more seed data
}
```

## Backup & Recovery

### Data Persistence

Data is stored in `/data/reporttree.db` (Docker mounted volume):

```yaml
# docker-compose.yml
services:
  pbihoster:
    volumes:
      - pbihoster_data:/data
      
volumes:
  pbihoster_data:
    driver: local
```

### Backup Procedure

```bash
# Backup database file
docker cp pbihoster:/data/reporttree.db ./backup/reporttree-$(date +%Y%m%d).db

# For production, use volume snapshots:
docker run --rm -v pbihoster_data:/data -v ./backup:/backup \
  alpine tar czf /backup/reporttree-$(date +%Y%m%d).tar.gz -C /data .
```

### Recovery Procedure

```bash
# Restore from backup
docker run --rm -v pbihoster_data:/data -v ./backup:/backup \
  alpine tar xzf /backup/reporttree-YYYYMMDD.tar.gz -C /data

# Restart application
docker-compose restart pbihoster
```

## Performance Considerations

### Indexing Strategy

- Indexes on frequently queried columns (Username, Email, ParentPageId, CreatedAt)
- Compound indexes for common filter combinations
- No index on binary data columns (BrandingAsset.Data)

### Query Optimization

```csharp
// ❌ INEFFICIENT: Load all, filter in memory
var users = _db.GetCollection<AppUser>()
    .FindAll()
    .Where(x => x.Roles.Contains("Admin"))
    .ToList();

// ✅ EFFICIENT: Filter at database level
var admins = _db.GetCollection<AppUser>()
    .Find(x => x.Roles.Contains("Admin"))
    .ToList();
```

### Monitoring

Health check verifies database accessibility:

```
GET /ready

Runs: LiteDatabase.GetCollectionNames()
Returns: 200 OK if successful, 503 if database unavailable
```

---

## Related Documentation

- [ARCHITECTURE.md](ARCHITECTURE.md) - System design overview
- [API.md](API.md) - REST API endpoints
- [SECURITY.md](SECURITY.md) - Data encryption and security
