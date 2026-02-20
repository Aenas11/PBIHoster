<template>
    <div class="panel audit-logs">
        <div class="header">
            <div>
                <h2>Audit Logs</h2>
                <p class="subtitle">Review security and activity logs.</p>
            </div>
            <div class="actions">
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
            <cds-checkbox
                label-text="RLS Changes Only"
                :checked="rlsOnly"
                @cds-checkbox-changed="toggleRLSFilter"
                :disabled="loading">
            </cds-checkbox>
        </div>

        <cds-table v-if="!loading" class="audit-table">
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
                <cds-table-row v-if="logs.length === 0">
                </cds-table-row>
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
const usernameFilter = ref('')
const resourceFilter = ref('')
const rlsOnly = ref(false)
const skip = ref(0)
const take = 50
const total = ref<number | null>(null)

const showPagination = computed(() => !usernameFilter.value && !resourceFilter.value && !rlsOnly.value)
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
}

function onResourceInput(e: Event) {
    resourceFilter.value = (e.target as HTMLInputElement).value
}

function toggleRLSFilter(e: CustomEvent) {
    rlsOnly.value = e.detail.checked
    skip.value = 0
    load()
}

function resetFilters() {
    usernameFilter.value = ''
    resourceFilter.value = ''
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
        if (usernameFilter.value.trim()) {
            const name = encodeURIComponent(usernameFilter.value.trim())
            logs.value = await api.get<AuditLog[]>(`/audit/user/${name}`)
            total.value = logs.value.length
            return
        }

        if (resourceFilter.value.trim()) {
            const resource = encodeURIComponent(resourceFilter.value.trim())
            logs.value = await api.get<AuditLog[]>(`/audit/resource/${resource}`)
            total.value = logs.value.length
            return
        }

        let url = `/audit?skip=${skip.value}&take=${take}`
        if (rlsOnly.value) {
            url += '&actionType=RLS_CONFIG_CHANGED'
        }

        const response = await api.get<{ logs: AuditLog[]; count: number }>(url)
        logs.value = response.logs
        total.value = response.count
    } catch (error) {
        console.error('Failed to load audit logs:', error)
        toast.error('Error', 'Failed to load audit logs')
    } finally {
        loading.value = false
    }
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
    grid-template-columns: 1fr 1fr auto;
    gap: 1rem;
    margin: 1rem 0 1.5rem;
    align-items: end;
}

@media (max-width: 768px) {
    .filters {
        grid-template-columns: 1fr;
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