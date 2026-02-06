# Product Roadmap & Implementation Plans

## Current Version: 0.3.0 (2025-02-06)

### v0.3.0 Highlights
- ✅ Semantic versioning via root `VERSION` file
- ✅ Demo mode with sample pages and data
- ✅ Onboarding walkthroughs (create pages, assign roles, configure themes)
- ✅ Comprehensive audit logging
- ✅ White-label customization (logo, app name, footer links)
- ✅ Favorites and bookmarks system
- ✅ Power BI embedding with RLS support
- ✅ Role-based access control (Admin, Editor, Viewer)
- ✅ Drag-and-drop page layout system
- ✅ Custom theme management
- ✅ JWT authentication with account lockout
- ✅ Rate limiting and security headers

---

## Planned Roadmap

### Phase 1: Quick Wins (2-3 weeks) - v0.4.0

Goal: Enable power users to manage datasets and implement advanced features quickly.

#### 1.1 Data Refresh Management
- **Status**: Design complete, implementation in progress
- **Features**:
  - Manual dataset refresh triggering
  - Scheduled refresh with cron editor
  - Refresh history and status dashboard
  - Email/webhook notifications on success/failure
  - Retry policy with exponential backoff
  - Power BI capacity constraint handling
- **Implementation**:
  - Backend: `DatasetRefreshService`, `RefreshSchedulerHostedService`
  - Models: `DatasetRefreshSchedule`, `DatasetRefreshRun`
  - API Endpoints: `/api/refreshes/*` (admin-only)
  - Frontend: Admin UI for schedule management
- **Acceptance Criteria**:
  - [x] Cron expression parsing with validation
  - [x] Time zone support (IANA identifiers)
  - [ ] UI for creating and managing schedules
  - [ ] Email notification service integration
  - [ ] Webhook support for external systems
  - [ ] History export (CSV/JSON)

#### 1.2 RLS (Row-Level Security) Management UI
- **Status**: Backend partial, frontend pending
- **Features**:
  - Visual interface for assigning RLS roles to users/groups
  - Per-report and per-component RLS configuration
  - Role mapping from external identity providers
  - Testing & validation UI
- **Implementation**:
  - Backend: Extend `Page` and `Layout` models for RLS metadata
  - Frontend: RLS configuration panel in page editor
  - Services: RLS role validation and assignment logic
- **Acceptance Criteria**:
  - [ ] UI to assign RLS roles per page/component
  - [ ] Support for dynamic RLS (user context-based)
  - [ ] Test RLS effectiveness via Power BI
  - [ ] Audit logging of RLS changes

#### 1.3 Token Refresh Endpoint
- **Status**: Design pending
- **Features**:
  - Extend JWT token validity without re-authentication
  - Refresh token rotation for security
  - Long-lived refresh tokens (7 days)
  - Short-lived access tokens (8 hours)
- **Implementation**:
  - API: `POST /auth/refresh` endpoint
  - Model: Refresh token storage with expiry tracking
  - Security: Refresh token invalidation on logout
- **Acceptance Criteria**:
  - [ ] Frontend sends refresh request before token expiry
  - [ ] Backend validates and rotates refresh tokens
  - [ ] All existing tokens remain valid after deployment

---

### Phase 2: Advanced Authentication (2-3 weeks) - v0.5.0

Goal: Support enterprise identity providers (Azure AD, Okta, Auth0, Clerk) and group synchronization.

#### 2.1 OpenID Connect (OIDC) / OAuth2 Integration
- **Status**: Architecture defined, implementation pending
- **Features**:
  - Support multiple OIDC providers simultaneously
  - Just-In-Time (JIT) user provisioning from external identity
  - Claim-based role mapping
  - Optional password policy enforcement for external users
  - Session management (cookies + JWT)
- **Implementation**:
  - Backend: OIDC handler in authentication middleware
  - Database: `AppUser.AuthProvider` and `AppUser.ExternalUserId` fields
  - Config: Environment variables for provider settings
  - Frontend: Login button with provider selection
- **Supported Providers**:
  - Azure AD / Entra ID (Microsoft)
  - Okta
  - Auth0
  - Clerk
  - Any OIDC-compliant provider
- **Acceptance Criteria**:
  - [ ] Login redirects to Azure AD, returns with claims
  - [ ] User auto-created with JIT sync
  - [ ] Roles mapped from group claims
  - [ ] Mixed mode (local + OIDC users) supported
  - [ ] Fallback for local admin user always works

#### 2.2 Azure AD Groups Synchronization
- **Status**: Design pending
- **Features**:
  - Periodic sync of Azure AD groups to internal groups
  - Manual sync trigger from admin UI
  - Group membership updates reflected in access control
  - Conflict resolution (local vs. AD groups)
- **Implementation**:
  - Service: `AzureAdSyncService` using Microsoft Graph API
  - Background: Scheduled hosted service for periodic sync
  - Admin UI: Manual sync button, sync status, audit trail
- **Acceptance Criteria**:
  - [ ] Groups synced from Azure AD on schedule (daily default)
  - [ ] Manual sync available in admin panel
  - [ ] Page access updated when group membership changes
  - [ ] Audit log captures all sync events

#### 2.3 Multi-Factor Authentication (MFA)
- **Status**: Design pending
- **Features**:
  - TOTP (Time-Based One-Time Password) support
  - Device-based MFA via Passwordless Sign-In (WebAuthn/FIDO2)
  - Recovery codes for account recovery
  - MFA enforcement per role or user
- **Implementation**:
  - Services: `MFAService`, `TOTPService`
  - Models: `UserMFASettings`, `RecoveryCode`
  - Admin: MFA enforcement policies
- **Acceptance Criteria**:
  - [ ] Admin can enforce MFA for Admin role
  - [ ] Users can enable TOTP via QR code
  - [ ] Recovery codes displayed and stored securely
  - [ ] MFA works with both local and OIDC auth

---

### Phase 3: Analytics & Monitoring (2 weeks) - v0.6.0

Goal: Provide visibility into platform usage and report performance.

#### 3.1 Usage Analytics Dashboard
- **Status**: Design pending
- **Features**:
  - Page view statistics (daily, weekly, monthly)
  - User activity heatmap (most-accessed pages)
  - Report rendering performance metrics
  - Active user sessions count
  - Engagement trends and retention
- **Implementation**:
  - Service: `UsageAnalyticsService` (aggregates events)
  - Models: `PageViewEvent`, `UserSessionEvent`
  - API: `/api/analytics/usage` (admin-only)
  - Frontend: Charts with time-range filtering
- **Acceptance Criteria**:
  - [ ] Page views tracked per page and date
  - [ ] User sessions tracked (login/logout)
  - [ ] Dashboard shows top pages by views
  - [ ] Time-range filters (daily, weekly, monthly)
  - [ ] Export analytics data (CSV/PDF)

#### 3.2 Performance Monitoring
- **Status**: Partial (basic /metrics endpoint exists)
- **Features**:
  - API response time tracking per endpoint
  - Power BI report rendering performance metrics
  - Database query performance insights
  - Infrastructure metrics (CPU, memory, disk)
  - Performance trend analysis
  - Alerting on performance degradation
- **Implementation**:
  - Middleware: Request timing capture
  - Services: Performance metrics aggregation
  - Prometheus: OpenTelemetry metrics export
  - Frontend: Performance dashboard
- **Acceptance Criteria**:
  - [ ] Metrics available at `/metrics` endpoint
  - [ ] Prometheus scraping configured
  - [ ] Grafana dashboard created
  - [ ] Alerts configured for high latency

#### 3.3 Health Monitoring & Alerting
- **Status**: Partial (health checks exist)
- **Features**:
  - Comprehensive health checks for all dependencies
  - Alerting integration (Slack, PagerDuty, email)
  - Incident tracking and root cause analysis
  - SLA tracking and reporting
- **Implementation**:
  - Extension: `IHealthCheck` implementations
  - Integration: Alerting service connectors
  - Models: Alert, Incident, SLAMetric
- **Acceptance Criteria**:
  - [ ] Health checks for DB, Power BI, Azure AD
  - [ ] Alerts sent to configured channels
  - [ ] Alert history maintained
  - [ ] SLA dashboards visible

---

### Phase 4: Collaboration & Governance (3 weeks) - v0.7.0

Goal: Enable teams to collaborate securely and maintain compliance.

#### 4.1 Comments & Annotations
- **Status**: Design pending
- **Features**:
  - Inline comments on pages and reports
  - @mention users for notifications
  - Comment threads with resolution status
  - Rich text editing (markdown support)
  - Comment export with context
  - Threaded discussions on reports
- **Implementation**:
  - Models: `Comment`, `CommentThread`, `Mention`
  - Service: `CommentService`, `NotificationService`
  - API: `/api/comments/*` endpoints
  - Frontend: Comment panel sidebar
- **Acceptance Criteria**:
  - [ ] Add/edit/delete comments
  - [ ] @mention users with notifications
  - [ ] Comments persist and display with timestamps
  - [ ] Comment history/audit trail

#### 4.2 Compliance & Data Governance
- **Status**: Design pending
- **Features**:
  - Data sensitivity labels (public, internal, confidential)
  - Access approval workflows
  - GDPR compliance tools (data export, right to be forgotten)
  - Compliance audit reports
  - Data retention policies
  - Regulatory reporting templates
- **Implementation**:
  - Models: `SensitivityLabel`, `AccessRequest`, `DataRetentionPolicy`
  - Services: Approval workflow engine, compliance reporting
  - API: Compliance and approval endpoints
  - Admin UI: Data governance dashboard
- **Acceptance Criteria**:
  - [ ] Label pages with sensitivity level
  - [ ] Access requests go through approval workflow
  - [ ] GDPR export available per user
  - [ ] Retention policies enforced
  - [ ] Compliance reports generated

#### 4.3 Report Versioning & Rollback
- **Status**: Design pending
- **Features**:
  - Automatic versioning of page layout changes
  - Version history with timestamps and author
  - Diff view between versions
  - Rollback to previous version
  - Change annotations (what changed and why)
  - Version branching (experimental layouts)
- **Implementation**:
  - Models: `PageVersion`, `VersionDiff`
  - Service: `VersioningService`
  - API: Version management endpoints
  - Frontend: Version history modal
- **Acceptance Criteria**:
  - [ ] Each layout change creates new version
  - [ ] Version history shows date, author, changes
  - [ ] Rollback restores exact layout
  - [ ] Diff view shows before/after

#### 4.4 Access Approval Workflows
- **Status**: Design pending
- **Features**:
  - Request-to-access flow for restricted pages
  - Approval by page owner or admin
  - Time-limited access grants (temporary access)
  - Audit trail of all approvals
  - Automatic expiry notifications
- **Implementation**:
  - Models: `AccessRequest`, `AccessGrant`
  - Service: Workflow engine, notifications
  - API: Request/approval endpoints
  - Frontend: My Access Requests view
- **Acceptance Criteria**:
  - [ ] Users can request access to restricted pages
  - [ ] Admin/owner receives notification and approves
  - [ ] Temporary access can be granted (e.g., 30 days)
  - [ ] Access automatically expires and user notified

---

### Phase 5: Advanced Features (4+ weeks) - v0.8.0+

Goal: Enterprise-scale features for large organizations and integrations.

#### 5.1 Scheduled Subscriptions
- **Status**: Design pending
- **Features**:
  - Email subscriptions to reports/pages
  - Scheduled delivery (daily, weekly, monthly)
  - Report snapshots and exports
  - Custom email templates
  - Subscriber management and unsubscribe
  - Delivery tracking and bounce handling
- **Implementation**:
  - Models: `Subscription`, `ScheduledDelivery`, `Snapshot`
  - Services: Subscription service, email service, export service
  - Background: Delivery scheduler
  - API: Subscription management endpoints
- **Acceptance Criteria**:
  - [ ] Users can subscribe to pages
  - [ ] Scheduled email delivery works
  - [ ] Report snapshots (PNG/PDF) included in email
  - [ ] Unsubscribe link in emails

#### 5.2 Advanced Scheduling
- **Status**: Partial (refresh scheduling exists)
- **Features**:
  - Flexible cron scheduling with UI builder
  - Holiday/blackout date support
  - Timezone-aware scheduling
  - Conditional triggers (e.g., on data update)
  - Chained tasks (dependencies)
- **Implementation**:
  - Enhancement: Extend existing `DatasetRefreshSchedule` model
  - Services: Advanced scheduler, condition evaluator
  - Frontend: Visual cron builder, calendar view
- **Acceptance Criteria**:
  - [ ] Complex cron expressions supported
  - [ ] Holidays excluded from schedules
  - [ ] Tasks can depend on other tasks
  - [ ] Visual calendar shows scheduled runs

#### 5.3 Multi-Tenancy Support
- **Status**: Design pending (deferred - clients host separate instances)
- **Features**:
  - Multiple isolated tenants per instance
  - Per-tenant branding and themes
  - Per-tenant data and users
  - Per-tenant Power BI workspaces
  - Tenant billing and metering
  - Tenant isolation at all layers
- **Note**: Current recommended deployment: one instance per client
  - Simplifies security and compliance
  - Easier backups and recovery
  - Clear data separation
- **Future**: Single-instance multi-tenancy may be offered as premium feature

#### 5.4 Backup & Disaster Recovery
- **Status**: Manual backups supported
- **Features**:
  - Automated encrypted backups
  - Geo-redundant backup storage
  - Point-in-time recovery
  - Backup integrity verification
  - Backup scheduling and retention policies
  - Recovery runbooks and testing
- **Implementation**:
  - Service: Backup orchestration
  - Integration: Cloud storage providers (Azure Storage, S3, GCS)
  - Admin UI: Backup management and recovery wizard
- **Acceptance Criteria**:
  - [ ] Daily automated backups
  - [ ] Encryption at rest (customer-managed keys)
  - [ ] Recovery tested and documented
  - [ ] SLA backup compliance tracking

#### 5.5 API Integrations
- **Status**: Partial (REST API exists)
- **Features**:
  - Webhooks for event notifications
  - GraphQL API as alternative to REST
  - gRPC for high-performance integrations
  - API rate limiting with tiers
  - OAuth2 for third-party integrations
  - Marketplace for extensions
- **Implementation**:
  - Framework: GraphQL (Hot Chocolate) or REST + Webhooks
  - Services: Webhook delivery, event bus
  - Platform: Extension marketplace
- **Acceptance Criteria**:
  - [ ] Webhooks fired on page create/update/delete
  - [ ] Third-party apps can authenticate via OAuth2
  - [ ] GraphQL queries work for all resources
  - [ ] Rate limits enforced per API tier

#### 5.6 Advanced Filtering & Search
- **Status**: Basic filtering exists
- **Features**:
  - Full-text search across all content
  - Advanced filtering (bool, range, date)
  - Saved filters and searches
  - Smart suggestions (AI/ML)
  - Autocomplete and fuzzy matching
- **Implementation**:
  - Enhancement: Elasticsearch or similar for full-text search
  - Services: Search indexing service
  - Frontend: Advanced search UI
- **Acceptance Criteria**:
  - [ ] Search finds pages and reports by text
  - [ ] Filters combinable with AND/OR logic
  - [ ] Search is fast (<100ms)
  - [ ] Saved searches accessible

#### 5.7 Database Abstraction & Multi-Database Support
- **Status**: Design pending (planned for v0.9.0+)
- **Goal**: Enable deployment flexibility with support for multiple database backends
- **Features**:
  
  **Phase A: Database Abstraction Layer** (v0.9.0)
  - Repository pattern refactoring (already started)
  - Generic data access abstraction layer
  - Entity Framework Core integration
  - Data mapper implementation
  - Migration support (schema versioning)
  - Acceptance Criteria:
    - [ ] All repositories implement IRepository<T> interface
    - [ ] EF Core replaces direct LiteDB queries
    - [ ] Database-agnostic repository code
    - [ ] Automated migrations for schema changes
  
  **Phase B: PostgreSQL Support** (v0.9.0)
  - Primary relational database support
  - Full-text search integration (PostgreSQL FTS)
  - JSONB support for flexible schemas
  - Multi-replica deployment capability
  - Features:
    - [ ] PostgreSQL connection pooling
    - [ ] Full audit log querying and filtering
    - [ ] Advanced analytics queries
    - [ ] Replication for HA (streaming replication)
  
  **Phase C: MySQL/MariaDB Support** (v1.0.0)
  - Alternative relational database
  - InnoDB full-text search
  - JSON column support
  - MySQL Cluster HA option
  - Features:
    - [ ] MySQL 8.0+ support
    - [ ] Connection pooling optimization
    - [ ] Compatibility with MySQL-managed services (RDS, CloudSQL, Azure MySQL)
  
  **Phase D: MongoDB Support** (v1.0.0+)
  - Document database option
  - Flexible schema support
  - Horizontal scaling via sharding
  - Features:
    - [ ] Document-to-model mapping
    - [ ] MongoDB Atlas compatibility
    - [ ] Replica set support for HA
    - [ ] Aggregation pipeline for analytics
  
  **Phase E: Cloud-Managed Databases** (v1.0.0+)
  - AWS RDS (PostgreSQL, MySQL, Aurora)
  - Azure Database (PostgreSQL, MySQL, CosmosDB)
  - Google Cloud SQL (PostgreSQL, MySQL)
  - Datastore-specific optimizations
  - Features:
    - [ ] Connection strings for managed services
    - [ ] Managed backup integration
    - [ ] Performance monitoring integration
    - [ ] Automatic scaling configuration guidance

- **Implementation Architecture**:
  ```
  Repository Layer (IUserRepository, IPageRepository, etc.)
                    ↓
  Generic Repository<T> Implementation
                    ↓
  Data Mapper (Entity → Domain Model)
                    ↓
  Database Provider (PostgreSQL, MySQL, MongoDB, LiteDB)
                    ↓
  Database (Specific implementation)
  ```

- **Configuration Changes**:
  - Environment variable: `DATABASE_PROVIDER` (litedb|postgresql|mysql|mongodb)
  - Environment variable: `DATABASE_CONNECTION_STRING`
  - Environment variable: `DATABASE_OPTIONS` (JSON for provider-specific settings)
  - appsettings.json: Database provider configuration section
  
- **Migration Strategy**:
  - LiteDB remains default for single-instance deployments
  - New deployments can choose database at setup
  - Existing installations: migration tool from LiteDB to PostgreSQL/MySQL
  - Zero-downtime migration with dual-write capability
  - Rollback support during migration window
  
- **Performance Targets**:
  - PostgreSQL: <50ms for typical queries
  - MySQL: <50ms for typical queries
  - MongoDB: <100ms for typical queries
  - Full-text search: <200ms
  - Audit log queries: <500ms for 30-day ranges

- **Scalability Benefits**:
  | Database | Max Replicas | HA | Full-Text Search | Analytics |
  |----------|--------------|----|--------------------|-----------|
  | LiteDB | 1 | Backup-based | No | Limited |
  | PostgreSQL | 3+ | Streaming replication | Native | Advanced |
  | MySQL | 3+ | Group replication | Plugin | Advanced |
  | MongoDB | 3+ | Replica sets | Atlas-based | Aggregation pipeline |

- **Acceptance Criteria**:
  - [ ] System boots with any supported database
  - [ ] All repositories work with all databases
  - [ ] Schema migrations run automatically on startup
  - [ ] Full-text search works on all databases
  - [ ] Audit logs queryable in all databases
  - [ ] Analytics dashboard works with all databases
  - [ ] Migration tool from LiteDB to PostgreSQL works end-to-end
  - [ ] Unit tests run against all databases
  - [ ] Performance benchmarks meet targets
  - [ ] Documentation for each database setup complete

- **Backward Compatibility**:
  - LiteDB default for existing deployments
  - No breaking changes to API
  - No UI changes required
  - Configuration-only switch
  - Opt-in migration path

---

## Cross-Phase Features

### Security Enhancements (Continuous)

- [ ] Implement secrets rotation automation
- [ ] Add compliance scanning (SOC2, HIPAA, GDPR)
- [ ] Extend audit logging to frontend events
- [ ] Implement API request signing
- [ ] Add encryption for in-flight and at-rest data
- [ ] Support Hardware Security Modules (HSM)
- [ ] Implement strict Content Security Policy (CSP)

### Performance Optimization (Continuous)

- [ ] Database query optimization and indexing
- [ ] Frontend bundle optimization and lazy loading
- [ ] Implement service worker for offline support
- [ ] Add HTTP/2 Server Push for critical resources
- [ ] Optimize database file size and compression
- [ ] Implement database connection pooling where applicable
- [ ] Add request/response compression

### Documentation (Continuous)

- [ ] Architecture Decision Records (ADRs)
- [ ] API client library SDKs (JavaScript, C#, Python)
- [ ] Video tutorials for common tasks
- [ ] Interactive API explorer (Swagger UI)
- [ ] Migration guides for major versions

### DevOps & Infrastructure (Continuous)

- [ ] Helm charts for Kubernetes deployments
- [ ] Terraform/IaC for cloud infrastructure
- [ ] CI/CD pipeline enhancements
- [ ] Automated dependency updates
- [ ] Container image optimization
- [ ] Multi-region deployment strategy

---

## Release Schedule (Tentative)

| Version | Target | Features |
|---------|--------|----------|
| v0.3.0 | Feb 2025 | ✅ Current - Demo mode, versioning, audit logs |
| v0.4.0 | Mar 2025 | Data refresh, RLS UI, token refresh |
| v0.5.0 | Apr 2025 | OIDC/OAuth2, Azure AD sync, MFA |
| v0.6.0 | May 2025 | Analytics, performance monitoring |
| v0.7.0 | Jun 2025 | Comments, compliance, versioning |
| v0.8.0 | Q3 2025 | Subscriptions, advanced scheduling |
| v0.9.0 | Q3 2025 | Database abstraction, PostgreSQL support |
| v1.0.0 | Q4 2025 | Stable GA release, MySQL/MongoDB support |
| v1.0.0+ | Q1 2026+ | Cloud-managed database support, continued scaling |

---

## Contribution Opportunities

We welcome contributions! High-priority areas for community input:

1. **Bug reports** - Found an issue? Open a GitHub issue
2. **Feature requests** - Want a feature? Discuss in Discussions
3. **Documentation** - Help improve guides and examples
4. **Code contributions** - See [CONTRIBUTING.md](CONTRIBUTING.md)
5. **Testing** - Help test beta features
6. **Translations** - Help localize UI and docs

---

## Feedback & Discussion

- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: Architecture, design, and planning discussions
- **Community Slack**: Join our community Slack for real-time discussion
- **Email**: contact@pbihoster.dev

---

## Related Documentation

- [CONTRIBUTING.md](CONTRIBUTING.md) - How to contribute
- [ARCHITECTURE.md](ARCHITECTURE.md) - System design
- [CHANGELOG.md](CHANGELOG.md) - Past releases
