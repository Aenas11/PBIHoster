<template>
    <div class="panel audit-logs">
        <div class="header">
            <div>
                <h2>Audit Logs</h2>
                <p class="subtitle">Review security and activity logs, then export the filtered result set.</p>
            </div>
            <div class="actions">
                <cds-button kind="tertiary" size="sm" @click="exportLogs('csv')"
                    :disabled="loading || exportFormat === 'csv'">
                    {{ exportFormat === 'csv' ? 'Exporting CSV...' : 'Export CSV' }}
                </cds-button>
                <cds-button kind="tertiary" size="sm" @click="exportLogs('pdf')"
                    :disabled="loading || exportFormat === 'pdf'">
                    {{ exportFormat === 'pdf' ? 'Exporting PDF...' : 'Export PDF' }}
                </cds-button>
                <cds-button kind="ghost" size="sm" @click="resetFilters" :disabled="loading">
                    Reset
                </cds-button>
                <cds-button kind="primary" size="sm" @click="load" :disabled="loading">
                    {{ loading ? 'Loading...' : 'Refresh' }}
                </cds-button>
            </div>
        </div>

        <div class="filters">
            <cds-text-input
                label="Filter by username"
                :value="usernameFilter"
                @input="onUsernameInput"
                placeholder="e.g., john.doe"
                :disabled="loading"
            ></cds-text-input>
            <cds-text-input
                label="Filter by resource"
                :value="resourceFilter"
                @input="onResourceInput"
                placeholder="e.g., Page:123"
                :disabled="loading"
            ></cds-text-input>
            <cds-select label-text="Action" :value="actionFilter" @cds-select-selected="onActionChange" :disabled="loading">
                <cds-select-item value="">All actions</cds-select-item>
                <cds-select-item v-for="action in actionOptions" :key="action" :value="action">{{ action }}</cds-select-item>
            </cds-select>
            <cds-select label-text="Success" :value="successFilter" @cds-select-selected="onSuccessChange" :disabled="loading">
                <cds-select-item value="all">All results</cds-select-item>
                <cds-select-item value="true">Success only</cds-select-item>
                <cds-select-item value="false">Failures only</cds-select-item>
            </cds-select>
            <cds-text-input
                label="From"
                type="datetime-local"
                :value="fromFilter"
                @input="onFromInput"
                :disabled="loading"
            ></cds-text-input>
            <cds-text-input
                label="To"
                type="datetime-local"
                :value="toFilter"
                @input="onToInput"
                :disabled="loading"
            ></cds-text-input>
            <cds-checkbox
                label-text="RLS Changes Only"
                :checked="rlsOnly"
                @cds-checkbox-changed="toggleRLSFilter"
                :disabled="loading">
            </cds-checkbox>
        </div>

        <cds-table v-if="!loading && logs.length > 0" class="audit-table">
            <cds-table-head>
                <cds-table-header-row>
                    <cds-table-header-cell>Timestamp</cds-table-header-cell>
                    <cds-table-header-cell>User</cds-table-header-cell>
                    <cds-table-header-cell>Action</cds-table-header-cell>
                    <cds-table-header-cell>Resource</cds-table-header-cell>
                    <cds-table-header-cell>Success</cds-table-header-cell>
                    <cds-table-header-cell>Details</cds-table-header-cell>
                </cds-table-header-row>
            </cds-table-head>
            <cds-table-body>
                <cds-table-row v-for="log in logs" :key="log.id">
                    <cds-table-cell>{{ formatDate(log.timestamp) }}</cds-table-cell>
                    <cds-table-cell>{{ log.username }}</cds-table-cell>
                    <cds-table-cell>{{ log.action }}</cds-table-cell>
                    <cds-table-cell>{{ log.resource }}</cds-table-cell>
                    <cds-table-cell>
                        <cds-tag :type="log.success ? 'green' : 'red'" size="sm">
                            {{ log.success ? 'Yes' : 'No' }}
                        </cds-tag>
                    </cds-table-cell>
                    <cds-table-cell>
                        <span class="muted">{{ log.details || '-' }}</span>
                    </cds-table-cell>
                </cds-table-row>
            </cds-table-body>
        </cds-table>

        <div v-else-if="loading" class="loading-state">
            <cds-loading></cds-loading>
            <p>Loading audit logs...</p>
        </div>

        <div v-else class="empty-state">
            No audit records matched the current filters.
        </div>

        <div class="pagination" v-if="showPagination">
            <cds-button kind="ghost" size="sm" @click="prevPage" :disabled="loading || !canPrev">
                Previous
            </cds-button>
            <div class="muted">Showing {{ pageRangeLabel }}</div>
            <cds-button kind="ghost" size="sm" @click="nextPage" :disabled="loading || !canNext">
                Next
            </cds-button>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api } from '@/services/api'
import { useToastStore } from '@/stores/toast'

import '@carbon/web-components/es/components/button/index.js'
import '@carbon/web-components/es/components/text-input/index.js'
import '@carbon/web-components/es/components/data-table/index.js'
import '@carbon/web-components/es/components/tag/index.js'
import '@carbon/web-components/es/components/checkbox/index.js'
import '@carbon/web-components/es/components/loading/index.js'
import '@carbon/web-components/es/components/select/index.js'

interface AuditLog {
    id: number
    username: string
    action: string
    resource: string
    details: string
    ipAddress: string
    userAgent: string
    timestamp: string
    success: boolean
}

const toast = useToastStore()
const logs = ref<AuditLog[]>([])
const loading = ref(false)
const exportFormat = ref<'csv' | 'pdf' | null>(null)
const usernameFilter = ref('')
const resourceFilter = ref('')
const actionFilter = ref('')
const successFilter = ref<'all' | 'true' | 'false'>('all')
const fromFilter = ref('')
const toFilter = ref('')
const rlsOnly = ref(false)
const skip = ref(0)
const take = 50
const total = ref<number | null>(null)
const actionOptions = [
    'AUDIT_EXPORT',
    'CHANGE_PASSWORD',
    'CREATE_COMMENT',
    'DATASET_REFRESH_HISTORY_EXPORT',
    'DATASET_REFRESH_RUN',
    'DELETE',
    'DELETE_COMMENT',
    'EMBED_DASHBOARD',
    'EMBED_REPORT',
    'RLS_CONFIG_CHANGED',
    'ROLLBACK_PAGE_LAYOUT',
    'SET_SENSITIVITY_LABEL',
    'UPDATE',
    'UPDATE_COMMENT'
]

const showPagination = computed(() => !loading.value && total.value !== null && total.value > take)
const canPrev = computed(() => skip.value > 0)
const canNext = computed(() => total.value !== null && skip.value + take < total.value)
const pageRangeLabel = computed(() => {
    if (total.value === null) return 'N/A'
    const start = total.value === 0 ? 0 : skip.value + 1
    const end = Math.min(skip.value + take, total.value)
    return `${start}-${end} of ${total.value}`
})

function onUsernameInput(e: Event) {
    usernameFilter.value = (e.target as HTMLInputElement).value
    skip.value = 0
}

function onResourceInput(e: Event) {
    resourceFilter.value = (e.target as HTMLInputElement).value
    skip.value = 0
}

function onActionChange(e: CustomEvent<{ value: string }>) {
    actionFilter.value = e.detail.value
    skip.value = 0
}

function onSuccessChange(e: CustomEvent<{ value: 'all' | 'true' | 'false' }>) {
    successFilter.value = e.detail.value
    skip.value = 0
}

function onFromInput(e: Event) {
    fromFilter.value = (e.target as HTMLInputElement).value
    skip.value = 0
}

function onToInput(e: Event) {
    toFilter.value = (e.target as HTMLInputElement).value
    skip.value = 0
}

function toggleRLSFilter(e: CustomEvent) {
    rlsOnly.value = e.detail.checked
    skip.value = 0
    if (rlsOnly.value) {
        actionFilter.value = 'RLS_CONFIG_CHANGED'
    } else if (actionFilter.value === 'RLS_CONFIG_CHANGED') {
        actionFilter.value = ''
    }
}

function resetFilters() {
    usernameFilter.value = ''
    resourceFilter.value = ''
    actionFilter.value = ''
    successFilter.value = 'all'
    fromFilter.value = ''
    toFilter.value = ''
    rlsOnly.value = false
    skip.value = 0
    load()
}

function formatDate(value: string) {
    return new Date(value).toLocaleString()
}

async function load() {
    try {
        loading.value = true
        const response = await api.get<{ logs: AuditLog[]; count: number }>(buildAuditUrl())
        logs.value = response.logs
        total.value = response.count
    } catch (error) {
        console.error('Failed to load audit logs:', error)
        toast.error('Error', 'Failed to load audit logs')
    } finally {
        loading.value = false
    }
}

async function exportLogs(format: 'csv' | 'pdf') {
    try {
        exportFormat.value = format

        const token = localStorage.getItem('token')
        const headers: HeadersInit = {}
        if (token) {
            headers['Authorization'] = `Bearer ${token}`
        }

        const response = await fetch(`/api${buildAuditUrl(true, format)}`, { headers })
        if (!response.ok) {
            throw new Error(`Export failed: ${response.status} ${response.statusText}`)
        }

        const blob = await response.blob()
        const url = URL.createObjectURL(blob)
        const disposition = response.headers.get('Content-Disposition')
        const fileName = disposition?.match(/filename\*?=(?:UTF-8'')?"?([^";]+)/i)?.[1] ?? `audit-export-${Date.now()}.${format}`
        const link = document.createElement('a')
        link.href = url
        link.download = decodeURIComponent(fileName)
        document.body.appendChild(link)
        link.click()
        document.body.removeChild(link)
        URL.revokeObjectURL(url)

        toast.success('Export complete', `Audit logs downloaded as ${format.toUpperCase()}`)
    } catch (error) {
        console.error('Failed to export audit logs:', error)
        toast.error('Export failed', 'Could not download audit logs')
    } finally {
        exportFormat.value = null
    }
}

function buildAuditUrl(forExport = false, format?: 'csv' | 'pdf') {
    const params = new URLSearchParams()

    if (!forExport) {
        params.set('skip', String(skip.value))
        params.set('take', String(take))
    }

    const username = usernameFilter.value.trim()
    const resource = resourceFilter.value.trim()
    const actionType = rlsOnly.value ? 'RLS_CONFIG_CHANGED' : actionFilter.value
    const fromUtc = toUtcIsoString(fromFilter.value)
    const toUtc = toUtcIsoString(toFilter.value)

    if (username) {
        params.set('username', username)
    }

    if (resource) {
        params.set('resource', resource)
    }

    if (actionType) {
        params.set('actionType', actionType)
    }

    if (successFilter.value !== 'all') {
        params.set('success', successFilter.value)
    }

    if (fromUtc) {
        params.set('fromUtc', fromUtc)
    }

    if (toUtc) {
        params.set('toUtc', toUtc)
    }

    if (forExport && format) {
        params.set('format', format)
        return `/audit/export?${params.toString()}`
    }

    return `/audit?${params.toString()}`
}

function toUtcIsoString(value: string) {
    if (!value) {
        return ''
    }

    return new Date(value).toISOString()
}

function nextPage() {
    if (!canNext.value) return
    skip.value += take
    load()
}

function prevPage() {
    if (!canPrev.value) return
    skip.value = Math.max(0, skip.value - take)
    load()
}

onMounted(() => {
    load()
})
</script>

<style scoped>
.audit-logs {
    min-width: 360px;
}

.header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
}

.actions {
    display: flex;
    gap: 0.5rem;
}

.subtitle {
    color: var(--cds-text-secondary);
    margin-top: 0.25rem;
}

.filters {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    gap: 1rem;
    margin: 1rem 0 1.5rem;
    align-items: end;
}

@media (max-width: 768px) {
    .header,
    .actions,
    .filters {
        grid-template-columns: 1fr;
    }

    .header {
        align-items: stretch;
    }

    .actions {
        width: 100%;
    }
}

.pagination {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    gap: 1rem;
    margin-top: 1rem;
}

.muted {
    color: var(--cds-text-secondary);
}

.loading-state {
    text-align: center;
    padding: 3rem 1rem;
    color: var(--cds-text-secondary);
}

.loading-state p {
    margin-top: 1rem;
}

.empty-state {
    text-align: center;
    padding: 2rem 1rem;
    color: var(--cds-text-secondary);
    font-style: italic;
}
</style>