<script setup lang="ts">

</script>

<template>
    <div class="help-view">
        <header class="hero">
            <div>
                <p class="eyebrow">Guided onboarding</p>
                <h1>Getting started with PBIHoster</h1>
                <p class="lede">
                    Follow these quick walkthroughs to create pages, assign roles, and configure themes. Demo mode ships with
                    a safe sample dataset so you can explore without tenant data.
                </p>
                <div class="hero-actions">
                    <cds-button kind="primary" href="/sample-data/sample-sales.csv" target="_blank">
                        Download sample dataset
                    </cds-button>
                    <cds-button kind="tertiary" href="https://github.com" disabled title="Coming soon">
                        Full docs (coming soon)
                    </cds-button>
                </div>
            </div>
        </header>

        <div class="grid">
            <cds-tile class="tile">
                <h3>Create pages</h3>
                <p>Switch to Edit Mode, add a page, and drop cards before saving a layout.</p>
                <img src="/onboarding/create-pages.svg" alt="Walkthrough showing how to create pages" loading="lazy" />
            </cds-tile>

            <cds-tile class="tile">
                <h3>Assign roles</h3>
                <p>Use Admin &gt; Users &amp; Groups to set Admin, Editor, or Viewer permissions.</p>
                <img src="/onboarding/assign-roles.svg" alt="Walkthrough showing how to assign roles" loading="lazy" />
            </cds-tile>

            <cds-tile class="tile">
                <h3>Configure themes</h3>
                <p>Open Theme Manager to tune Carbon tokens or select a preset before saving.</p>
                <img src="/onboarding/configure-themes.svg" alt="Walkthrough showing how to configure themes" loading="lazy" />
            </cds-tile>

            <cds-tile class="tile">
                <h3>Sample report preview</h3>
                <p>Use the bundled dataset to build KPIs, then replace this preview with your live embed.</p>
                <img src="/onboarding/sample-report.svg" alt="Static preview of the sample sales report" loading="lazy" />
            </cds-tile>
        </div>

        <cds-inline-notification
            kind="info"
            title="Demo mode"
            subtitle="When Demo Mode is enabled in Admin Settings, the navigation includes Demo Overview and Sample Insights pages backed by the safe starter dataset."
        />
    </div>
</template>

<style scoped lang="scss">
.help-view {
    padding: 2rem;
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
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

.grid {
    display: grid;
    gap: 1.5rem;
    grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
}

.tile {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.tile img {
    width: 100%;
    border-radius: 8px;
    border: 1px solid var(--cds-border-subtle-01);
    background: var(--cds-layer-01);
}
</style>
