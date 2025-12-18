<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import { powerBIService } from '../services/powerbi.service'
import PowerBIEmbed from '../components/PowerBIEmbed.vue'
import type { ReportDto, EmbedTokenResponseDto } from '../types/powerbi'
import '@carbon/web-components/es/components/loading/index.js'
import '@carbon/web-components/es/components/tabs/index.js'

const route = useRoute()
const reports = ref<ReportDto[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const embedData = ref<EmbedTokenResponseDto | null>(null)
const embedLoading = ref(false)

// Get workspaceId from Page model (passed via route meta or props)
const workspaceId = computed(() => {
    // Can come from route query or page configuration
    return route.query.workspaceId as string || route.meta.workspaceId as string
})

// Get current report from query param
const currentReportId = computed(() => route.query.reportId as string)

// Find current report
const currentReport = computed(() =>
    reports.value.find(r => r.id === currentReportId.value)
)

async function loadReports() {
    if (!workspaceId.value) {
        error.value = 'Workspace ID not provided'
        loading.value = false
        return
    }

    loading.value = true
    try {
        reports.value = await powerBIService.getReports(workspaceId.value)
        error.value = null
    } catch (e: any) {
        console.error('Failed to load reports:', e)
        error.value = 'Failed to load reports from workspace'
    } finally {
        loading.value = false
    }
}

async function loadEmbedToken() {
    if (!workspaceId.value || !currentReportId.value) return

    embedLoading.value = true
    try {
        embedData.value = await powerBIService.getReportEmbedToken(
            workspaceId.value,
            currentReportId.value,
            parseInt(route.params.id as string) // pageId for authorization
        )
    } catch (e: any) {
        console.error('Failed to load embed token:', e)
        error.value = 'Failed to load report'
    } finally {
        embedLoading.value = false
    }
}

watch(currentReportId, () => {
    if (currentReportId.value) {
        loadEmbedToken()
    }
})

onMounted(async () => {
    await loadReports()
    if (currentReportId.value) {
        await loadEmbedToken()
    } else if (reports.value.length > 0) {
        // Auto-select first report if none selected
        const firstReport = reports.value[0]
        await loadEmbedToken()
    }
})
</script>

<template>
    <div class="workspace-reports-view">
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
            <cds-tabs :value="currentReportId || reports[0].id">
                <cds-tab v-for="report in reports" :key="report.id" :id="`tab-${report.id}`"
                    :target="`panel-${report.id}`" :value="report.id"
                    @click="$router.push({ query: { ...route.query, reportId: report.id } })">
                    {{ report.name }}
                </cds-tab>
            </cds-tabs>

            <div class="report-content">
                <div v-if="embedLoading" class="loading-container">
                    <cds-loading></cds-loading>
                </div>
                <PowerBIEmbed v-else-if="embedData && currentReport" :embed-url="embedData.embedUrl"
                    :access-token="embedData.accessToken" embed-type="report" />
            </div>
        </div>
    </div>
</template>

<style scoped>
.workspace-reports-view {
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
    padding: 3rem;
    gap: 1rem;
}

.reports-container {
    display: flex;
    flex-direction: column;
    height: 100%;
}

.report-content {
    flex: 1;
    min-height: 0;
    position: relative;
}
</style>
