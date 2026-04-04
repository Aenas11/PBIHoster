<script setup lang="ts">
import { computed, ref, onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { powerBIService } from '../../services/powerbi.service'
import PowerBIEmbed from '../PowerBIEmbed.vue'
import { useThemeStore } from '../../stores/theme'
import { reportClientError } from '../../services/monitoring'
import type { DashboardComponentProps } from '../../types/components'
import type { ReportDto, EmbedTokenResponseDto } from '../../types/powerbi'
import '@carbon/web-components/es/components/loading/index.js'
import '@carbon/web-components/es/components/tabs/index.js'
import '@carbon/web-components/es/components/button/index.js'

const props = defineProps<DashboardComponentProps>()
const route = useRoute()
const router = useRouter()
const themeStore = useThemeStore()

const reports = ref<ReportDto[]>([])
const loading = ref(true)
const runtimeError = ref<string | null>(null)
const embedData = ref<EmbedTokenResponseDto | null>(null)
const embedLoading = ref(false)
const loadRetryCount = ref(0)
const embedRetryCount = ref(0)
const maxRetries = 2
const retryTimer = ref<number | null>(null)

const syncWithAppTheme = computed(() => (props.config.syncWithAppTheme as boolean) ?? false)

const mappedContrastMode = computed(() => {
    if (!syncWithAppTheme.value) return undefined
    return themeStore.currentTheme === 'g90' || themeStore.currentTheme === 'g100'
        ? 'HighContrastBlack'
        : undefined
})

const mappedBackground = computed<'Default' | 'Transparent'>(() => {
    if (!syncWithAppTheme.value) return 'Transparent'
    return 'Transparent'
})

const configError = computed(() => {
    const { workspaceId } = props.config
    if (!workspaceId) {
        return 'Please configure Workspace ID.'
    }
    return null
})

const shouldRetry = (e: unknown) => {
    const message = e instanceof Error ? e.message : String(e)
    return /Network|fetch|timeout|429|5\d\d/i.test(message)
}

const clearRetryTimer = () => {
    if (retryTimer.value) {
        clearTimeout(retryTimer.value)
        retryTimer.value = null
    }
}

// Get reportId from query parameter
const currentReportId = ref<string>((route.query.reportId as string) || '')

async function loadReports() {
    const { workspaceId } = props.config
    if (!workspaceId) {
        runtimeError.value = null
        reports.value = []
        loading.value = false
        return
    }

    loading.value = true
    try {
        reports.value = await powerBIService.getReports(workspaceId as string)
        runtimeError.value = null
        loadRetryCount.value = 0
        clearRetryTimer()

        // Auto-select first report if none selected
        if (reports.value.length > 0 && !currentReportId.value) {
            const firstReport = reports.value[0]
            if (firstReport) {
                currentReportId.value = firstReport.id
                await loadEmbedToken(firstReport.id)
            }
        } else if (currentReportId.value) {
            await loadEmbedToken(currentReportId.value)
        }
    } catch (e: unknown) {
        console.error('Failed to load reports:', e)
        runtimeError.value = 'Failed to load reports from workspace'
        reports.value = []
        if (loadRetryCount.value < maxRetries && shouldRetry(e)) {
            loadRetryCount.value++
            clearRetryTimer()
            retryTimer.value = window.setTimeout(() => loadReports(), 1500 * loadRetryCount.value)
        } else {
            await reportClientError({
                message: 'Failed to load reports from workspace',
                info: 'PowerBIWorkspaceComponent reports fetch failed after retries',
                context: {
                    componentId: props.id,
                    workspaceId: workspaceId as string,
                    retryCount: loadRetryCount.value
                }
            })
            loadRetryCount.value = 0
        }
    } finally {
        loading.value = false
    }
}

async function loadEmbedToken(reportId: string) {
    const { workspaceId, enableRLS, rlsRoles, syncWithAppTheme } = props.config
    if (!workspaceId || !reportId) return

    embedLoading.value = true
    try {
        embedData.value = await powerBIService.getReportEmbedToken(
            workspaceId as string,
            reportId,
            undefined, // pageId
            enableRLS as boolean | undefined,
            rlsRoles as string[] | undefined,
            syncWithAppTheme as boolean | undefined
        )
        runtimeError.value = null
        embedRetryCount.value = 0
        clearRetryTimer()
    } catch (e: unknown) {
        console.error('Failed to load embed token:', e)
        runtimeError.value = 'Failed to load report'
        embedData.value = null
        if (embedRetryCount.value < maxRetries && shouldRetry(e)) {
            embedRetryCount.value++
            clearRetryTimer()
            retryTimer.value = window.setTimeout(() => loadEmbedToken(reportId), 1500 * embedRetryCount.value)
        } else {
            await reportClientError({
                message: 'Failed to load workspace report embed token',
                info: 'PowerBIWorkspaceComponent embed token failed after retries',
                context: {
                    componentId: props.id,
                    workspaceId: workspaceId as string,
                    reportId,
                    retryCount: embedRetryCount.value
                }
            })
            embedRetryCount.value = 0
        }
    } finally {
        embedLoading.value = false
    }
}

const retryNow = () => {
    clearRetryTimer()
    loadRetryCount.value = 0
    embedRetryCount.value = 0
    if (!reports.value.length) {
        loadReports()
        return
    }
    if (currentReportId.value) {
        loadEmbedToken(currentReportId.value)
    } else {
        loadReports()
    }
}

function selectReport(reportId: string) {
    currentReportId.value = reportId
    // Update query parameter
    router.push({
        query: { ...route.query, reportId }
    })
    loadEmbedToken(reportId)
}

watch(() => route.query.reportId, (newReportId) => {
    if (newReportId && newReportId !== currentReportId.value) {
        currentReportId.value = newReportId as string
        loadEmbedToken(newReportId as string)
    }
})

onMounted(() => {
    loadReports()
})

onUnmounted(() => {
    clearRetryTimer()
})
</script>

<template>
    <div class="workspace-reports-component">
        <div v-if="configError" class="state-message state-message--config">
            <p>{{ configError }}</p>
        </div>

        <div v-else-if="loading" class="state-message state-message--loading">
            <cds-loading></cds-loading>
            <p>Loading reports...</p>
        </div>

        <div v-else-if="runtimeError" class="state-message state-message--error">
            <p>{{ runtimeError }}</p>
            <cds-button size="sm" kind="tertiary" @click="retryNow">Retry</cds-button>
        </div>

        <div v-else-if="reports.length === 0" class="state-message state-message--empty">
            <p>No reports found in this workspace</p>
            <cds-button size="sm" kind="tertiary" @click="retryNow">Reload</cds-button>
        </div>

        <div v-else class="reports-container">
            <div class="reports-tabs">
                <cds-tabs :value="currentReportId">
                    <cds-tab v-for="report in reports" :key="report.id" :id="`tab-${report.id}`" :value="report.id"
                        @click="selectReport(report.id)">
                        {{ report.name }}
                    </cds-tab>
                </cds-tabs>
            </div>

            <div class="report-content">
                <div v-if="embedLoading" class="loading-container">
                    <cds-loading></cds-loading>
                </div>
                <PowerBIEmbed v-else-if="embedData" :embed-url="embedData.embedUrl"
                    :access-token="embedData.accessToken" :report-id="currentReportId" embed-type="report"
                    :background="mappedBackground" :contrast-mode="mappedContrastMode" />
            </div>
        </div>
    </div>
</template>

<style scoped>
.workspace-reports-component {
    height: 100%;
    display: flex;
    flex-direction: column;
}

.state-message {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 2rem;
    gap: 1rem;
}

.state-message--error {
    color: #da1e28;
    background: #fff0f1;
}

.state-message--config {
    color: #8a3c00;
    background: #fff8e1;
}

.reports-container {
    display: flex;
    flex-direction: column;
    height: 100%;
}

.reports-tabs {
    flex-shrink: 0;
}

.report-content {
    flex: 1;
    min-height: 0;
    position: relative;
}
</style>
