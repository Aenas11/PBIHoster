## 0.5.0 (2026-06-12)

### Highlights
- Added analytics improvements, including daily trend series, device breakdown, and CSV export.
- Completed external authentication phase 2 with OIDC discovery/challenge/callback and claim-to-role/group sync.
- Added admin mapping endpoints and UI for non-secret external identity configuration.
- Added comments feature toggle and backend enforcement when comments are disabled.
- Added page layout version history and rollback endpoints with UI support.
- Added sensitivity labels with optional enforcement on page create/update.
- Added compliance audit export with CSV/PDF support and advanced filtering.
- Added relational persistence support (Sqlite, SQL Server, PostgreSQL) with migration-aware startup.

### Upgrade notes
- Update the root VERSION file before merging to main. CI will publish image tags using this value.
- For relational providers, verify provider-specific migrations are available in your deployment image.
- Review CHANGELOG.md for detailed behavior changes before production rollout.
- Back up your database before upgrading and validate readiness using /ready after deployment.
