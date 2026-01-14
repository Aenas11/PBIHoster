<script setup lang="ts">
import { computed, ref } from 'vue'
import { powerBIService } from '../../services/powerbi.service'
import type { PowerBIDiagnosticResultDto, PowerBIDiagnosticCheckDto, DiagnosticStatus } from '../../types/powerbi'
import '@carbon/web-components/es/components/button/index.js'
import '@carbon/web-components/es/components/tag/index.js'
import '@carbon/web-components/es/components/tile/index.js'
import '@carbon/web-components/es/components/notification/index.js'
import '@carbon/web-components/es/components/structured-list/index.js'
import { Launch16 } from '@carbon/icons-vue'
import { useToastStore } from '../../stores/toast'

const toast = useToastStore()
const diagnostics = ref<PowerBIDiagnosticResultDto | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)

const statusTagType = (status: DiagnosticStatus) => {
    switch (status) {
        case 'Success':
            return 'green'
        case 'Warning':
            return 'warm-gray'
        case 'Error':
            return 'red'
    }
}

const hasErrors = computed(() => diagnostics.value?.checks.some(c => c.status === 'Error'))

async function runDiagnostics() {
    loading.value = true
    error.value = null
    try {
        diagnostics.value = await powerBIService.runDiagnostics()
        if (hasErrors.value) {
            toast.error('Diagnostics completed', 'Issues were found. Review the checklist for actions.')
        } else {
            toast.success('Diagnostics completed', 'Power BI connectivity is healthy.')
        }
    } catch (e: unknown) {
        const message = e instanceof Error ? e.message : 'Unable to run diagnostics.'
        error.value = message
        toast.error('Diagnostics failed', message)
    } finally {
        loading.value = false
    }
}

const friendlyStatus = (status: DiagnosticStatus) => {
    if (status === 'Success') return 'OK'
    if (status === 'Warning') return 'Warning'
    return 'Error'
}

const sortedChecks = computed<PowerBIDiagnosticCheckDto[]>(() => {
    if (!diagnostics.value) return []
    const priority: DiagnosticStatus[] = ['Error', 'Warning', 'Success']
    return [...diagnostics.value.checks].sort(
        (a, b) => priority.indexOf(a.status) - priority.indexOf(b.status)
    )
})
</script>

<template>
    <cds-tile class="diagnostics-card">
        <div class="diagnostics-header">
            <div>
                <h2>Power BI Diagnostics</h2>
                <p>Validate the configured service principal can reach your Power BI tenant and workspaces.</p>
            </div>
            <cds-button kind="primary" size="sm" :disabled="loading" @click="runDiagnostics">
                {{ loading ? 'Running...' : 'Run diagnostics' }}
            </cds-button>
        </div>

        <cds-inline-notification v-if="error" kind="error" :open="true" title="Diagnostics failed"
            :subtitle="error"></cds-inline-notification>

        <div v-if="diagnostics" class="diagnostics-content">
            <div class="summary-row">
                <div class="summary-item">
                    <span class="label">Overall status</span>
                    <cds-tag :type="diagnostics.success ? 'green' : 'red'" size="sm">
                        {{ diagnostics.success ? 'Healthy' : 'Attention needed' }}
                    </cds-tag>
                </div>
                <div class="summary-item">
                    <span class="label">Workspaces visible</span>
                    <span class="value">{{ diagnostics.workspaces?.length ?? 0 }}</span>
                </div>
                <div class="summary-item">
                    <span class="label">Reports found</span>
                    <span class="value">{{ diagnostics.reports?.length ?? 0 }}</span>
                </div>
                <div v-if="diagnostics.azurePortalLink" class="summary-item">
                    <span class="label">Azure portal</span>
                    <a :href="diagnostics.azurePortalLink" target="_blank" rel="noreferrer" class="link">
                        View app registration
                        <Launch16 class="link-icon" />
                    </a>
                </div>
            </div>

            <div class="checks">
                <h3>Checklist</h3>
                <cds-structured-list>
                    <cds-structured-list-head>
                        <cds-structured-list-header-row>
                            <cds-structured-list-header-cell>Status</cds-structured-list-header-cell>
                            <cds-structured-list-header-cell>Check</cds-structured-list-header-cell>
                            <cds-structured-list-header-cell>Details</cds-structured-list-header-cell>
                        </cds-structured-list-header-row>
                    </cds-structured-list-head>
                    <cds-structured-list-body>
                        <cds-structured-list-row v-for="check in sortedChecks" :key="check.name">
                            <cds-structured-list-cell>
                                <cds-tag :type="statusTagType(check.status)" size="sm">
                                    {{ friendlyStatus(check.status) }}
                                </cds-tag>
                            </cds-structured-list-cell>
                            <cds-structured-list-cell>
                                <div class="check-title">{{ check.name }}</div>
                                <div v-if="check.resolution" class="check-resolution">
                                    Resolution: {{ check.resolution }}
                                    <a v-if="check.docsUrl" :href="check.docsUrl" target="_blank" rel="noreferrer"
                                        class="link">
                                        Docs
                                    </a>
                                </div>
                            </cds-structured-list-cell>
                            <cds-structured-list-cell class="check-detail">
                                {{ check.detail }}
                            </cds-structured-list-cell>
                        </cds-structured-list-row>
                    </cds-structured-list-body>
                </cds-structured-list>
            </div>

            <div class="lists">
                <div class="list">
                    <h3>Workspaces</h3>
                    <p v-if="!diagnostics.workspaces?.length" class="muted">No workspaces were returned.</p>
                    <ul v-else>
                        <li v-for="workspace in diagnostics.workspaces" :key="workspace.id">
                            <span class="item-title">{{ workspace.name }}</span>
                            <span class="muted">({{ workspace.id }})</span>
                        </li>
                    </ul>
                </div>
                <div class="list">
                    <h3>Reports</h3>
                    <p v-if="!diagnostics.reports?.length" class="muted">No reports were returned.</p>
                    <ul v-else>
                        <li v-for="report in diagnostics.reports" :key="report.id">
                            <span class="item-title">{{ report.name }}</span>
                            <span class="muted">({{ report.id }})</span>
                        </li>
                    </ul>
                </div>
            </div>
        </div>

        <p v-else class="placeholder">
            Run diagnostics to see workspace availability and embed readiness.
        </p>
    </cds-tile>
</template>

<style scoped lang="scss">
.diagnostics-card {
    padding: 1.5rem;
    min-width: 320px;
    width: 540px;
    max-width: 100%;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.diagnostics-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;

    h2 {
        margin: 0;
    }

    p {
        margin: 0.25rem 0 0 0;
        color: var(--cds-text-secondary);
        font-size: 0.9rem;
    }
}

.diagnostics-content {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.summary-row {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
    gap: 0.75rem;
}

.summary-item {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.label {
    font-size: 0.85rem;
    color: var(--cds-text-secondary);
}

.value {
    font-size: 1.1rem;
    font-weight: 600;
}

.checks {
    h3 {
        margin: 0 0 0.5rem 0;
    }
}

.check-title {
    font-weight: 600;
    margin-bottom: 0.25rem;
}

.check-detail {
    color: var(--cds-text-secondary);
    font-size: 0.95rem;
}

.check-resolution {
    font-size: 0.9rem;
    color: var(--cds-text-secondary);
}

.lists {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 1rem;
}

.list {
    h3 {
        margin: 0 0 0.5rem 0;
    }

    ul {
        padding-left: 1rem;
        margin: 0;
    }

    li {
        margin-bottom: 0.35rem;
    }
}

.muted {
    color: var(--cds-text-secondary);
    font-size: 0.85rem;
}

.item-title {
    font-weight: 600;
    margin-right: 0.35rem;
}

.link {
    color: var(--cds-link-primary, #0f62fe);
    text-decoration: none;
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;

    &:hover {
        text-decoration: underline;
    }
}

.link-icon {
    flex-shrink: 0;
}

.placeholder {
    color: var(--cds-text-secondary);
}
</style>
