# Changelog

All notable changes to this project will be documented in this file. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- Completed external authentication Section 2 delivery:
	- OIDC provider discovery, challenge, and callback endpoints.
	- External claim-to-role and claim-to-group sync services.
	- Frontend SSO login and callback handling.
	- Admin non-secret external mapping configuration endpoints and UI.
- Added backend integration tests for external auth admin mapping settings endpoints.
- Added admin-configurable comments feature toggle via `App.CommentsEnabled` in static settings.
- Added backend enforcement to fully disable comments endpoints when comments feature is turned off.
- Added page layout version history with rollback endpoints (`/api/pages/{id}/versions` and rollback action).
- Added backend integration tests for page version save and rollback behavior.
- Added page sensitivity labels (`Public`, `Internal`, `Confidential`, `Restricted`) with backend validation and UI badges.
- Added admin toggle `App.EnforceSensitivityLabels` to require labels on page create/update.
- Added compliance audit export endpoint with CSV/PDF output and filter support for date range, user, action type, resource, and success state.
- Added backend integration tests for audit export filtering and invalid range handling.
- Added Admin Audit Logs panel enhancements for advanced filtering and direct CSV/PDF export downloads.
- Added Phase 5.1 database abstraction layer with provider-based persistence selection (`LiteDb`, `Sqlite`, `SqlServer`, `PostgreSql`).
- Added EF Core relational persistence stack (`AppDbContext`, JSON value converters/comparers, and `Ef*Repository` implementations for existing repository interfaces).
- Added relational database readiness health check and startup schema initialization for relational providers.
- Added provider-agnostic branding asset file repository for relational mode (`Database:BrandingAssetsPath`).
- Added relational integration tests covering auth/page CRUD flow and audit export behavior under Sqlite.
- Added repo-local EF tooling manifest for reproducible migration commands (`dotnet-tools.json`).
- Added initial committed relational migration set for `AppDbContext`.
- Added optional containerized relational provider smoke tests for SQL Server and PostgreSQL (enabled with `RUN_RELATIONAL_CONTAINER_TESTS=true`).
- Added dedicated provider-specific EF migration projects for SQL Server and PostgreSQL with initial migration chains.

### Changed
- Updated project documentation to reflect implemented external auth architecture, security boundaries, and admin mapping behavior.
- Updated comments UI to respect static feature settings and hide/toggle comments controls when disabled.
- Updated page edit UI with version history panel, diff summary, and one-click rollback.
- Updated page create/edit flow and navigation/page headers to display and manage sensitivity labels.
- Updated audit APIs and security documentation to cover compliance export behavior.
- Updated roadmap documentation to reflect completed Phase 4 delivery and renumber remaining access-approval work.
- Updated backend startup wiring to switch persistence registrations by configuration while retaining LiteDB as the default.
- Updated relational startup initialization to apply committed EF migrations for `Sqlite` and keep safe schema bootstrapping for the other relational providers until dedicated migration sets are added.
- Updated relational startup initialization to select provider-specific migration assemblies (`Sqlite`, `SqlServer`, `PostgreSql`) and apply migrations when assemblies are available, with `EnsureCreated()` fallback if unavailable.
- Updated server build/publish pipeline to include provider migration assemblies automatically for runtime migration discovery.
- Updated server dependency graph with EF Core provider packages and aligned Azure.Identity package version.
- Updated audit export string handling to safely process null values in CSV/PDF paths.

## [0.3.0] - 2025-02-11
### Added
- Introduced semver version tracking via `VERSION` file to align app releases with CI-generated Docker image tags.
- Added quick-start demo content including sample page layout and seeded report/dashboard metadata for showcasing capabilities without tenant data.
- Implemented “Demo Mode” toggle in settings and API to switch between live tenant data and demo content.
- Linked new onboarding walkthroughs for creating pages, assigning roles, and configuring themes from README and in-app help.
