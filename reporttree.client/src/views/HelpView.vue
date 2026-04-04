<script setup lang="ts">

</script>

<template>
    <div class="help-view">
        <header class="hero">
            <div>
                <p class="eyebrow">In-app documentation</p>
                <h1>PBIHoster help center</h1>
                <p class="lede">
                    This guide covers the complete setup flow for admins and editors: creating pages, embedding Power BI,
                    configuring RLS, assigning permissions, and troubleshooting common issues.
                </p>
                <div class="hero-actions">
                    <cds-button kind="primary" href="/sample-data/sample-sales.csv" target="_blank">
                        Download sample dataset
                    </cds-button>
                    <cds-button kind="tertiary" href="#quickstart">
                        Open quickstart
                    </cds-button>
                </div>
            </div>
        </header>

        <div class="quick-links" id="quickstart">
            <a href="#first-page">Create first page</a>
            <a href="#powerbi-setup">Power BI embed setup</a>
            <a href="#rls">RLS configuration</a>
            <a href="#access">Users and groups</a>
            <a href="#themes">Themes and branding</a>
            <a href="#troubleshooting">Troubleshooting</a>
        </div>

        <section class="docs-layout">
            <article class="docs-main">
                <cds-tile class="doc-section" id="first-page">
                    <h2>1. Create your first page</h2>
                    <ol>
                        <li>Open the left navigation and select <strong>Edit Mode</strong>.</li>
                        <li>Create a page and pick an icon that matches the report domain.</li>
                        <li>Add components to the grid (for example: Power BI report, favorites, notes).</li>
                        <li>Save layout changes and verify the page in view mode.</li>
                    </ol>
                    <img src="/onboarding/create-pages.svg" alt="Walkthrough showing how to create pages" loading="lazy" />
                </cds-tile>

                <cds-tile class="doc-section" id="powerbi-setup">
                    <h2>2. Configure Power BI embedding</h2>
                    <p>
                        Use the Power BI components from the page editor and provide workspace, report, and dataset identifiers
                        from your Power BI tenant.
                    </p>
                    <ul>
                        <li>Report component: for a single report with optional page name and filters.</li>
                        <li>Dashboard component: for tile-based overview dashboards.</li>
                        <li>Workspace component: for browsing reports in one workspace.</li>
                        <li>Enable <strong>Sync with app theme</strong> only when your report visuals support matching contrast.</li>
                    </ul>
                    <img src="/onboarding/sample-report.svg" alt="Static preview of the sample sales report" loading="lazy" />
                </cds-tile>

                <cds-tile class="doc-section" id="rls">
                    <h2>3. Row-level security (RLS)</h2>
                    <p>
                        RLS is configured per Power BI component. Use it when users should only see records tied to their role,
                        group, or identity.
                    </p>
                    <ol>
                        <li>Define roles in the Power BI dataset.</li>
                        <li>In component settings, map app users/groups to dataset roles.</li>
                        <li>Validate with a Viewer account before publishing broadly.</li>
                    </ol>
                    <p class="callout">
                        Tip: Start with least-privileged roles and add access gradually.
                    </p>
                </cds-tile>

                <cds-tile class="doc-section" id="access">
                    <h2>4. Users, groups, and page access</h2>
                    <p>Use Admin -&gt; Users &amp; Groups to manage permissions and combine role-based and page-level access.</p>
                    <ul>
                        <li><strong>Admin</strong>: full platform management.</li>
                        <li><strong>Editor</strong>: create/update pages and content.</li>
                        <li><strong>Viewer</strong>: read-only access to assigned content.</li>
                    </ul>
                    <p>
                        Private pages require authentication and explicit membership (allowed users or allowed groups).
                        Public pages can be opened without login.
                    </p>
                    <img src="/onboarding/assign-roles.svg" alt="Walkthrough showing how to assign roles" loading="lazy" />
                </cds-tile>

                <cds-tile class="doc-section" id="themes">
                    <h2>5. Themes and branding</h2>
                    <p>
                        Use Theme Manager to switch presets or create a custom look. Branding settings control logo, footer text,
                        and identity assets.
                    </p>
                    <ul>
                        <li>Apply theme changes globally from Admin settings.</li>
                        <li>Use the header switcher for user-level light/dark preference.</li>
                        <li>Confirm embedded report readability after major color changes.</li>
                    </ul>
                    <img src="/onboarding/configure-themes.svg" alt="Walkthrough showing how to configure themes" loading="lazy" />
                </cds-tile>

                <cds-tile class="doc-section" id="troubleshooting">
                    <h2>6. Troubleshooting quick checks</h2>
                    <div class="checklist">
                        <h3>Report does not load</h3>
                        <ul>
                            <li>Confirm workspace/report IDs are correct.</li>
                            <li>Verify service principal credentials in server settings.</li>
                            <li>Use retry in the component error state after transient failures.</li>
                        </ul>
                    </div>
                    <div class="checklist">
                        <h3>Access denied</h3>
                        <ul>
                            <li>Check user role and page-level allow lists.</li>
                            <li>Confirm group assignment and token refresh.</li>
                            <li>Review audit entries for authorization failures.</li>
                        </ul>
                    </div>
                    <div class="checklist">
                        <h3>Theme mismatch</h3>
                        <ul>
                            <li>Toggle Sync with app theme for the affected component.</li>
                            <li>Validate contrast on both light and dark themes.</li>
                            <li>Reopen component settings to ensure values were persisted.</li>
                        </ul>
                    </div>
                </cds-tile>
            </article>

            <aside class="docs-side">
                <cds-tile class="side-card">
                    <h3>Admin startup checklist</h3>
                    <ol>
                        <li>Create at least one Admin and one Viewer account.</li>
                        <li>Set Power BI configuration and test one embed.</li>
                        <li>Create a public landing page and one private page.</li>
                        <li>Configure branding assets and default theme.</li>
                        <li>Enable Demo Mode only for sandbox environments.</li>
                    </ol>
                </cds-tile>
                <cds-tile class="side-card">
                    <h3>Editor publishing checklist</h3>
                    <ol>
                        <li>Build layout in Edit Mode and save.</li>
                        <li>Validate component settings and data scope.</li>
                        <li>Test with Viewer account for final access checks.</li>
                        <li>Confirm mobile rendering on common breakpoints.</li>
                    </ol>
                </cds-tile>
            </aside>
        </section>

        <cds-inline-notification
            kind="info"
            title="Demo mode"
            subtitle="When Demo Mode is enabled in Admin Settings, navigation includes sample pages backed by the starter dataset. Keep it disabled in production environments with live tenant data."
        />
    </div>
</template>

<style scoped lang="scss">
.help-view {
    padding: 2rem;
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

.hero {
    background: var(--cds-layer-01);
    border: 1px solid var(--cds-border-subtle-01);
    border-radius: 12px;
    padding: 1.5rem;
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.06);
}

.eyebrow {
    margin: 0;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    font-size: 0.75rem;
    color: var(--cds-text-secondary);
    font-weight: 600;
}

h1 {
    margin: 0.25rem 0 0.5rem 0;
    font-size: 2rem;
    color: var(--cds-text-primary);
}

.lede {
    margin: 0 0 1rem 0;
    color: var(--cds-text-secondary);
    max-width: 760px;
    line-height: 1.5;
}

.hero-actions {
    display: flex;
    gap: 0.75rem;
    flex-wrap: wrap;
}

.quick-links {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    gap: 0.75rem;
}

.quick-links a {
    display: block;
    padding: 0.65rem 0.8rem;
    border-radius: 8px;
    background: var(--cds-layer-01);
    border: 1px solid var(--cds-border-subtle-01);
    color: var(--cds-link-primary);
    text-decoration: none;
    font-size: 0.875rem;
}

.quick-links a:hover {
    background: var(--cds-layer-hover-01);
}

.docs-layout {
    display: grid;
    grid-template-columns: minmax(0, 2.3fr) minmax(280px, 1fr);
    gap: 1rem;
    align-items: start;
}

.docs-main {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.doc-section {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.doc-section h2 {
    margin: 0;
    font-size: 1.25rem;
}

.doc-section p,
.doc-section li {
    line-height: 1.5;
    color: var(--cds-text-secondary);
}

.doc-section ul,
.doc-section ol {
    margin: 0;
    padding-left: 1.2rem;
    display: grid;
    gap: 0.25rem;
}

.doc-section img {
    width: 100%;
    border-radius: 8px;
    border: 1px solid var(--cds-border-subtle-01);
    background: var(--cds-layer-01);
}

.docs-side {
    position: sticky;
    top: 1rem;
    display: grid;
    gap: 1rem;
}

.side-card {
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
}

.side-card h3 {
    margin: 0;
}

.side-card ol {
    margin: 0;
    padding-left: 1.2rem;
    display: grid;
    gap: 0.35rem;
}

.checklist + .checklist {
    margin-top: 0.25rem;
}

.checklist h3 {
    margin: 0;
    font-size: 1rem;
}

.callout {
    padding: 0.7rem 0.8rem;
    border-radius: 8px;
    background: var(--cds-layer-02);
    border: 1px solid var(--cds-border-subtle-02);
}

@media (max-width: 900px) {
    .help-view {
        padding: 1rem;
    }

    .docs-layout {
        grid-template-columns: 1fr;
    }

    .docs-side {
        position: static;
    }
}
</style>
