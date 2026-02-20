<template>
    <div class="panel refresh-manager">
        <div class="header">
            <div>
                <h2>Data Refresh</h2>
                <p class="subtitle">Manage dataset refresh schedules and history.</p>
            </div>
            <cds-button kind="primary" size="sm" @click="openCreate">
                Create schedule
            </cds-button>
        </div>

        <cds-table v-if="schedules.length > 0" class="schedules-table">
            <cds-table-head>
                <cds-table-header-row>
                    <cds-table-header-cell>Name</cds-table-header-cell>
                    <cds-table-header-cell>Dataset</cds-table-header-cell>
                    <cds-table-header-cell>Cron</cds-table-header-cell>
                    <cds-table-header-cell>Time zone</cds-table-header-cell>
                    <cds-table-header-cell>Enabled</cds-table-header-cell>
                    <cds-table-header-cell>Latest</cds-table-header-cell>
                    <cds-table-header-cell>Actions</cds-table-header-cell>
                </cds-table-header-row>
            </cds-table-head>
            <cds-table-body>
                <cds-table-row v-for="schedule in schedules" :key="schedule.id">
                    <cds-table-cell>
                        <button class="link" @click="selectSchedule(schedule)">
                            {{ schedule.name }}
                        </button>
                        <div class="muted">{{ schedule.workspaceId }}</div>
                    </cds-table-cell>
                    <cds-table-cell>
                        <div>{{ schedule.datasetId }}</div>
                        <div v-if="schedule.reportId" class="muted">Report: {{ schedule.reportId }}</div>
                    </cds-table-cell>
                    <cds-table-cell>{{ schedule.cron }}</cds-table-cell>
                    <cds-table-cell>{{ schedule.timeZone }}</cds-table-cell>
                    <cds-table-cell>
                        <cds-tag :type="schedule.enabled ? 'green' : 'cool-gray'" size="sm">
                            {{ schedule.enabled ? 'Enabled' : 'Disabled' }}
                        </cds-tag>
                    </cds-table-cell>
                    <cds-table-cell>
                        <cds-tag :type="latestStatusTag(schedule.datasetId)" size="sm">
                            {{ latestStatusLabel(schedule.datasetId) }}
                        </cds-tag>
                    </cds-table-cell>
                    <cds-table-cell>
                        <div class="actions">
                            <cds-button size="sm" kind="ghost" @click="runNow(schedule)">
                                Run now
                            </cds-button>
                            <cds-button size="sm" kind="ghost" @click="openEdit(schedule)">
                                Edit
                            </cds-button>
                            <cds-button size="sm" kind="ghost" @click="toggle(schedule)">
                                {{ schedule.enabled ? 'Disable' : 'Enable' }}
                            </cds-button>
                            <cds-button size="sm" kind="danger-ghost" @click="remove(schedule)">
                                Delete
                            </cds-button>
                        </div>
                    </cds-table-cell>
                </cds-table-row>
            </cds-table-body>
        </cds-table>

        <div v-else class="empty-state">No schedules yet</div>

        <div v-if="selectedSchedule" class="history">
            <div class="history-header">
                <h3>Refresh history - {{ selectedSchedule.name }}</h3>
                <a :href="refreshService.exportHistoryUrl(selectedSchedule.datasetId, 'csv')"
                    class="export-link" :aria-label="`Download refresh history for ${selectedSchedule.name} as CSV`" download>
                    <cds-button size="sm" kind="ghost">Export CSV</cds-button>
                </a>
            </div>

            <cds-table v-if="history.length > 0">
                <cds-table-head>
                    <cds-table-header-row>
                        <cds-table-header-cell>Status</cds-table-header-cell>
                        <cds-table-header-cell>Requested</cds-table-header-cell>
                        <cds-table-header-cell>Duration</cds-table-header-cell>
                        <cds-table-header-cell>Retries</cds-table-header-cell>
                        <cds-table-header-cell>Failure reason</cds-table-header-cell>
                    </cds-table-header-row>
                </cds-table-head>
                <cds-table-body>
                    <cds-table-row v-for="run in history" :key="run.id">
                        <cds-table-cell>
                            <cds-tag :type="statusTag(run.status)" size="sm">
                                {{ run.status }}
                            </cds-tag>
                        </cds-table-cell>
                        <cds-table-cell>{{ formatDate(run.requestedAtUtc) }}</cds-table-cell>
                        <cds-table-cell>{{ formatDuration(run.durationMs) }}</cds-table-cell>
                        <cds-table-cell>{{ run.retriesAttempted }}</cds-table-cell>
                        <cds-table-cell>
                            <span class="muted">{{ run.failureReason || '-' }}</span>
                        </cds-table-cell>
                    </cds-table-row>
                </cds-table-body>
            </cds-table>

            <div v-else class="empty-state">No refresh history</div>
        </div>

        <cds-modal :open="showModal" @cds-modal-closed="closeModal">
            <cds-modal-header>
                <cds-modal-close-button></cds-modal-close-button>
                <cds-modal-label>Data Refresh</cds-modal-label>
                <cds-modal-heading>{{ editing ? 'Edit schedule' : 'Create schedule' }}</cds-modal-heading>
            </cds-modal-header>
            <cds-modal-body>
                <div class="lookup">
                    <h4>Power BI lookup</h4>
                    <cds-select label-text="Workspace" :value="lookupWorkspace"
                        @cds-select-selected="onLookupWorkspace">
                        <cds-select-item value="" disabled>Select workspace</cds-select-item>
                        <cds-select-item v-for="ws in workspaces" :key="ws.id" :value="ws.id">
                            {{ ws.name }}
                        </cds-select-item>
                    </cds-select>

                    <cds-select label-text="Report" :value="lookupReport" @cds-select-selected="onLookupReport"
                        :disabled="!lookupWorkspace">
                        <cds-select-item value="" disabled>Select report (optional)</cds-select-item>
                        <cds-select-item v-for="rep in reports" :key="rep.id" :value="rep.id">
                            {{ rep.name }}
                        </cds-select-item>
                    </cds-select>

                    <cds-select label-text="Dataset" :value="lookupDataset"
                        @cds-select-selected="lookupDataset = $event.detail.value" :disabled="!lookupWorkspace">
                        <cds-select-item value="" disabled>Select dataset</cds-select-item>
                        <cds-select-item v-for="ds in datasets" :key="ds.id" :value="ds.id">
                            {{ ds.name }}
                        </cds-select-item>
                    </cds-select>

                    <cds-button size="sm" kind="ghost" @click="applyLookup" :disabled="!lookupWorkspace">
                        Use selection
                    </cds-button>
                </div>

                <cds-text-input label="Name" :value="form.name" @input="onNameInput"></cds-text-input>
                <cds-text-input label="Workspace ID" :value="form.workspaceId"
                    @input="onWorkspaceInput"></cds-text-input>
                <cds-text-input label="Dataset ID" :value="form.datasetId" @input="onDatasetInput"></cds-text-input>
                <cds-text-input label="Report ID (optional)" :value="form.reportId"
                    @input="onReportInput"></cds-text-input>
                <cds-text-input label="Page ID (optional)" :value="form.pageId" @input="onPageInput"></cds-text-input>
                <cds-text-input label="Cron" helper-text="Example: 0 3 * * *" :value="form.cron"
                    @input="onCronInput"></cds-text-input>
                <cds-text-input label="Time zone" helper-text="IANA, e.g. UTC" :value="form.timeZone"
                    @input="onTimeZoneInput"></cds-text-input>
                <cds-text-input label="Retry count" type="number" :value="form.retryCount"
                    @input="onRetryCountInput"></cds-text-input>
                <cds-text-input label="Retry backoff (seconds)" type="number" :value="form.retryBackoffSeconds"
                    @input="onRetryBackoffInput"></cds-text-input>
                <cds-text-input label="Notification targets"
                    helper-text="Comma-separated, use email: or webhook: prefixes" :value="notifyTargetsText"
                    @input="onNotifyTargetsInput"></cds-text-input>
                <cds-toggle label-text="Enable schedule" :checked="form.enabled"
                    @cds-toggle-changed="onEnabledToggle"></cds-toggle>
                <cds-toggle label-text="Notify on success" :checked="form.notifyOnSuccess"
                    @cds-toggle-changed="onNotifySuccessToggle"></cds-toggle>
                <cds-toggle label-text="Notify on failure" :checked="form.notifyOnFailure"
                    @cds-toggle-changed="onNotifyFailureToggle"></cds-toggle>
            </cds-modal-body>
            <cds-modal-footer>
                <cds-modal-footer-button kind="secondary" @click="closeModal">Cancel</cds-modal-footer-button>
                <cds-modal-footer-button kind="primary" @click="save" :disabled="!form.name || !form.datasetId">
                    {{ editing ? 'Save' : 'Create' }}
                </cds-modal-footer-button>
            </cds-modal-footer>
        </cds-modal>
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { refreshService } from '@/services/refresh.service'
import { powerBIService } from '@/services/powerbi.service'
import type {
    DatasetRefreshScheduleDto,
    DatasetRefreshRunDto,
    RefreshNotificationTargetDto
} from '@/types/refresh'
import type { DatasetDto, ReportDto, WorkspaceDto } from '@/types/powerbi'
import { useToastStore } from '@/stores/toast'

import '@carbon/web-components/es/components/button/index.js'
import '@carbon/web-components/es/components/data-table/index.js'
import '@carbon/web-components/es/components/modal/index.js'
import '@carbon/web-components/es/components/select/index.js'
import '@carbon/web-components/es/components/text-input/index.js'
import '@carbon/web-components/es/components/tag/index.js'
import '@carbon/web-components/es/components/toggle/index.js'

const toast = useToastStore()
const schedules = ref<DatasetRefreshScheduleDto[]>([])
const history = ref<DatasetRefreshRunDto[]>([])
const selectedScheduleId = ref<string | null>(null)
const latestRuns = ref<Record<string, DatasetRefreshRunDto | null>>({})
const workspaces = ref<WorkspaceDto[]>([])
const reports = ref<ReportDto[]>([])
const datasets = ref<DatasetDto[]>([])
const lookupWorkspace = ref('')
const lookupReport = ref('')
const lookupDataset = ref('')
const showModal = ref(false)
const editing = ref(false)

const form = ref({
    id: '' as string | null,
    name: '',
    workspaceId: '',
    datasetId: '',
    reportId: '',
    pageId: '',
    cron: '0 3 * * *',
    timeZone: 'UTC',
    retryCount: 2,
    retryBackoffSeconds: 120,
    enabled: true,
    notifyOnSuccess: false,
    notifyOnFailure: true,
    notifyTargets: [] as RefreshNotificationTargetDto[]
})

const notifyTargetsText = ref('')

const selectedSchedule = computed(() =>
    schedules.value.find(s => s.id === selectedScheduleId.value) || null
)

function statusTag(status: string) {
    if (status === 'Succeeded') return 'green'
    if (status === 'Failed') return 'red'
    if (status === 'InProgress') return 'blue'
    return 'cool-gray'
}

function latestStatusTag(datasetId: string) {
    const latest = latestRuns.value[datasetId]
    if (!latest) return 'cool-gray'
    return statusTag(latest.status)
}

function latestStatusLabel(datasetId: string) {
    const latest = latestRuns.value[datasetId]
    return latest?.status ?? 'Unknown'
}

function formatDate(value?: string | null) {
    if (!value) return '-'
    return new Date(value).toLocaleString()
}

function formatDuration(durationMs?: number | null) {
    if (!durationMs) return '-'
    const seconds = Math.round(durationMs / 1000)
    return `${seconds}s`
}

function openCreate() {
    editing.value = false
    form.value = {
        id: null,
        name: '',
        workspaceId: '',
        datasetId: '',
        reportId: '',
        pageId: '',
        cron: '0 3 * * *',
        timeZone: 'UTC',
        retryCount: 2,
        retryBackoffSeconds: 120,
        enabled: true,
        notifyOnSuccess: false,
        notifyOnFailure: true,
        notifyTargets: []
    }
    notifyTargetsText.value = ''
    lookupWorkspace.value = ''
    lookupReport.value = ''
    lookupDataset.value = ''
    ensureWorkspacesLoaded()
    showModal.value = true
}

function openEdit(schedule: DatasetRefreshScheduleDto) {
    editing.value = true
    form.value = {
        id: schedule.id,
        name: schedule.name,
        workspaceId: schedule.workspaceId,
        datasetId: schedule.datasetId,
        reportId: schedule.reportId ?? '',
        pageId: schedule.pageId ? schedule.pageId.toString() : '',
        cron: schedule.cron,
        timeZone: schedule.timeZone,
        retryCount: schedule.retryCount,
        retryBackoffSeconds: schedule.retryBackoffSeconds,
        enabled: schedule.enabled,
        notifyOnSuccess: schedule.notifyOnSuccess,
        notifyOnFailure: schedule.notifyOnFailure,
        notifyTargets: schedule.notifyTargets ?? []
    }
    notifyTargetsText.value = schedule.notifyTargets
        .map(t => `${t.type.toLowerCase()}:${t.target}`)
        .join(', ')
    lookupWorkspace.value = schedule.workspaceId
    lookupReport.value = schedule.reportId ?? ''
    lookupDataset.value = schedule.datasetId
    ensureWorkspacesLoaded()
    if (schedule.workspaceId) {
        loadWorkspaceAssets(schedule.workspaceId)
    }
    showModal.value = true
}

function closeModal() {
    showModal.value = false
}

function onNameInput(e: Event) { form.value.name = (e.target as HTMLInputElement).value }
function onWorkspaceInput(e: Event) { form.value.workspaceId = (e.target as HTMLInputElement).value }
function onDatasetInput(e: Event) { form.value.datasetId = (e.target as HTMLInputElement).value }
function onReportInput(e: Event) { form.value.reportId = (e.target as HTMLInputElement).value }
function onPageInput(e: Event) { form.value.pageId = (e.target as HTMLInputElement).value }
function onCronInput(e: Event) { form.value.cron = (e.target as HTMLInputElement).value }
function onTimeZoneInput(e: Event) { form.value.timeZone = (e.target as HTMLInputElement).value }
function onRetryCountInput(e: Event) { form.value.retryCount = Number((e.target as HTMLInputElement).value) }
function onRetryBackoffInput(e: Event) { form.value.retryBackoffSeconds = Number((e.target as HTMLInputElement).value) }
function onNotifyTargetsInput(e: Event) { notifyTargetsText.value = (e.target as HTMLInputElement).value }
function onEnabledToggle(e: CustomEvent) { form.value.enabled = Boolean(e.detail?.checked ?? e.detail?.value) }
function onNotifySuccessToggle(e: CustomEvent) { form.value.notifyOnSuccess = Boolean(e.detail?.checked ?? e.detail?.value) }
function onNotifyFailureToggle(e: CustomEvent) { form.value.notifyOnFailure = Boolean(e.detail?.checked ?? e.detail?.value) }

async function ensureWorkspacesLoaded() {
    if (workspaces.value.length > 0) return
    try {
        workspaces.value = await powerBIService.getWorkspaces()
    } catch (error) {
        console.error(error)
    }
}

async function loadWorkspaceAssets(workspaceId: string) {
    if (!workspaceId) return
    try {
        const [reportsResult, datasetsResult] = await Promise.all([
            powerBIService.getReports(workspaceId),
            powerBIService.getDatasets(workspaceId)
        ])
        reports.value = reportsResult
        datasets.value = datasetsResult
    } catch (error) {
        console.error(error)
    }
}

async function onLookupWorkspace(e: CustomEvent) {
    lookupWorkspace.value = e.detail.value
    lookupReport.value = ''
    lookupDataset.value = ''
    await loadWorkspaceAssets(lookupWorkspace.value)
}

function onLookupReport(e: CustomEvent) {
    lookupReport.value = e.detail.value
    const report = reports.value.find(r => r.id === lookupReport.value)
    if (report) {
        lookupDataset.value = report.datasetId
    }
}

function applyLookup() {
    if (lookupWorkspace.value) {
        form.value.workspaceId = lookupWorkspace.value
    }
    if (lookupDataset.value) {
        form.value.datasetId = lookupDataset.value
    }
    if (lookupReport.value) {
        form.value.reportId = lookupReport.value
    }
}

function parseNotifyTargets(text: string): RefreshNotificationTargetDto[] {
    if (!text.trim()) return []
    return text
        .split(',')
        .map(entry => entry.trim())
        .filter(Boolean)
        .map(entry => {
            const parts = entry.split(':')
            const rawType = parts[0] ?? ''
            const remainder = parts.slice(1).join(':').trim()
            if (!remainder) {
                return { type: 'Email', target: rawType.trim() }
            }
            const type = rawType.trim().toLowerCase() === 'webhook' ? 'Webhook' : 'Email'
            return { type, target: remainder }
        })
}

async function loadSchedules() {
    schedules.value = await refreshService.getSchedules()
    const uniqueDatasets = Array.from(new Set(schedules.value.map(s => s.datasetId)))
    const latestEntries = await Promise.all(
        uniqueDatasets.map(async datasetId => {
            try {
                const runs = await refreshService.getHistory(datasetId, 0, 1)
                return [datasetId, runs[0] ?? null] as const
            } catch (error) {
                console.error(error)
                return [datasetId, null] as const
            }
        })
    )
    latestRuns.value = Object.fromEntries(latestEntries)
    const first = schedules.value[0]
    if (first && !selectedScheduleId.value) {
        selectSchedule(first)
    }
}

async function selectSchedule(schedule: DatasetRefreshScheduleDto) {
    selectedScheduleId.value = schedule.id
    history.value = await refreshService.getHistory(schedule.datasetId, 0, 20)
}

async function runNow(schedule: DatasetRefreshScheduleDto) {
    try {
        await refreshService.runDatasetRefresh(schedule.datasetId, {
            workspaceId: schedule.workspaceId,
            reportId: schedule.reportId ?? undefined,
            pageId: schedule.pageId ?? undefined
        })
        toast.success('Refresh started', 'The dataset refresh was triggered.')
        await selectSchedule(schedule)
    } catch (error) {
        console.error(error)
    }
}

async function save() {
    const payload = {
        name: form.value.name,
        workspaceId: form.value.workspaceId,
        datasetId: form.value.datasetId,
        reportId: form.value.reportId || undefined,
        pageId: form.value.pageId ? Number(form.value.pageId) : undefined,
        enabled: form.value.enabled,
        cron: form.value.cron,
        timeZone: form.value.timeZone,
        retryCount: form.value.retryCount,
        retryBackoffSeconds: form.value.retryBackoffSeconds,
        notifyOnSuccess: form.value.notifyOnSuccess,
        notifyOnFailure: form.value.notifyOnFailure,
        notifyTargets: parseNotifyTargets(notifyTargetsText.value)
    }

    if (editing.value && form.value.id) {
        await refreshService.updateSchedule(form.value.id, payload)
        toast.success('Schedule updated', form.value.name)
    } else {
        await refreshService.createSchedule(payload)
        toast.success('Schedule created', form.value.name)
    }

    showModal.value = false
    await loadSchedules()
}

async function toggle(schedule: DatasetRefreshScheduleDto) {
    await refreshService.toggleSchedule(schedule.id)
    await loadSchedules()
}

async function remove(schedule: DatasetRefreshScheduleDto) {
    if (!confirm(`Delete schedule "${schedule.name}"?`)) return
    await refreshService.deleteSchedule(schedule.id)
    await loadSchedules()
}

onMounted(async () => {
    try {
        await loadSchedules()
    } catch (error) {
        console.error(error)
    }
})
</script>

<style scoped>
.panel {
    padding: 12px;
    border: 1px solid #ddd;
    border-radius: 6px;
    width: 100%;
}

.header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    margin-bottom: 1rem;
}

.subtitle {
    margin: 0.25rem 0 0 0;
    color: var(--cds-text-secondary);
    font-size: 0.9rem;
}

.schedules-table {
    margin-top: 0.5rem;
}

.actions {
    display: flex;
    flex-wrap: wrap;
    gap: 0.25rem;
}

.muted {
    color: var(--cds-text-02, #525252);
    font-size: 0.85rem;
}

.link {
    background: none;
    border: none;
    color: var(--cds-link-primary, #0f62fe);
    cursor: pointer;
    padding: 0;
    font-size: 0.95rem;
}

.history {
    margin-top: 1.5rem;
}

.history-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    margin-bottom: 0.75rem;
}

.history-header h3 {
    margin: 0;
}

.export-link {
    text-decoration: none;
}

.lookup {
    border: 1px dashed var(--cds-border-subtle, #c6c6c6);
    border-radius: 6px;
    padding: 0.75rem;
    margin-bottom: 1rem;
    display: grid;
    gap: 0.5rem;
}

.lookup h4 {
    margin: 0;
    font-size: 0.95rem;
}

.empty-state {
    padding: 1rem 0;
    color: var(--cds-text-02, #525252);
}
</style>
