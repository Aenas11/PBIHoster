<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { powerBIService } from '../../services/powerbi.service'
import PowerBIEmbed from '../PowerBIEmbed.vue'
import type { DashboardComponentProps } from '../../types/components'
import type { ReportDto, EmbedTokenResponseDto } from '../../types/powerbi'
import '@carbon/web-components/es/components/loading/index.js'
import '@carbon/web-components/es/components/tabs/index.js'

const props = defineProps<DashboardComponentProps>()
const route = useRoute()
const router = useRouter()

const reports = ref<ReportDto[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const embedData = ref<EmbedTokenResponseDto | null>(null)
const embedLoading = ref(false)

// Get reportId from query parameter
const currentReportId = ref<string>((route.query.reportId as string) || '')

async function loadReports() {
    const { workspaceId } = props.config
    if (!workspaceId) {
        error.value = 'Workspace ID not configured'
        loading.value = false
        return
    }

    loading.value = true
    try {
        reports.value = await powerBIService.getReports(workspaceId as string)
        error.value = null

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
        error.value = 'Failed to load reports from workspace'
    } finally {
        loading.value = false
    }
}

async function loadEmbedToken(reportId: string) {
    const { workspaceId, enableRLS, rlsRoles } = props.config
    if (!workspaceId || !reportId) return

    embedLoading.value = true
    try {
        embedData.value = await powerBIService.getReportEmbedToken(
            workspaceId as string,
            reportId,
            undefined, // pageId
            enableRLS as boolean | undefined,
            rlsRoles as string[] | undefined
        )
    } catch (e: unknown) {
        console.error('Failed to load embed token:', e)
        error.value = 'Failed to load report'
    } finally {
        embedLoading.value = false
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
</script>

<template>
    <div class="workspace-reports-component">
        <div v-if="loading" class="loading-container">
            <cds-loading></cds-loading>
            <p>Loading reports...</p>
        </div>

        <div v-else-if="error" class="error-container">
            <p>{{ error }}</p>
        </div>

        <div v-else-if="reports.length === 0" class="empty-container">
            <p>No reports found in this workspace</p>
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
                    :access-token="embedData.accessToken" :report-id="currentReportId" embed-type="report" />
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

.loading-container,
.error-container,
.empty-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 2rem;
    gap: 1rem;
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
