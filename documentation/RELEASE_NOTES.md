## 0.3.0 (2025-02-11)

### Highlights
- Introduced semantic versioning via the root `VERSION` file and updated CI to tag Docker images with versioned artifacts.
- Added CHANGELOG tracking (Keep a Changelog format) to document releases.
- Delivered demo mode with sample pages, dataset, and static report preview to explore capabilities without tenant data.
- Added onboarding walkthroughs (create pages, assign roles, configure themes) and linked them from README and the in-app Help view.

### Upgrade notes
- Toggle `App.DemoModeEnabled` in Admin → Settings to enable or disable demo content.
- Update `VERSION` before merging to main; CI will tag images with `v<version>` automatically.
- Use the sample dataset under `reporttree.client/public/sample-data/` for quick Power BI imports.
