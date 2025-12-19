<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import PowerBIEmbed from '../PowerBIEmbed.vue'
import { powerBIService } from '../../services/powerbi.service'
import { useToastStore } from '../../stores/toast'
import type { DashboardComponentProps } from '../../types/components'
import type { EmbedTokenResponseDto } from '../../types/powerbi'

const props = defineProps<DashboardComponentProps>()
const toast = useToastStore()

const embedData = ref<EmbedTokenResponseDto | null>(null)
const error = ref<string | null>(null)

const getUserFriendlyError = (e: unknown): string => {
    if (e instanceof Error) {
        if (e.message.includes('401') || e.message.includes('Unauthorized')) {
            return 'Power BI authentication failed. Please check your credentials.'
        }
        if (e.message.includes('404') || e.message.includes('Not Found')) {
            return 'Dashboard not found. It may have been deleted or you may not have access.'
        }
        if (e.message.includes('403') || e.message.includes('Forbidden')) {
            return 'You do not have permission to access this dashboard.'
        }
        if (e.message.includes('Network') || e.message.includes('fetch')) {
            return 'Network error. Please check your connection and try again.'
        }
    }
    return 'Failed to load dashboard. Please try again later.'
}

const loadEmbedData = async () => {
    const { workspaceId, dashboardId } = props.config
    if (!workspaceId || !dashboardId) {
        if (workspaceId === undefined && dashboardId === undefined) return
        error.value = "Workspace and Dashboard not configured."
        return
    }

    try {
        embedData.value = await powerBIService.getDashboardEmbedToken(workspaceId as string, dashboardId as string)
        error.value = null
    } catch (e: unknown) {
        const friendlyMessage = getUserFriendlyError(e)
        error.value = friendlyMessage
        toast.error('Dashboard Load Failed', friendlyMessage, 7000)
        console.error('Power BI Dashboard Error:', e)
    }
}

onMounted(() => {
    loadEmbedData()
})

watch(() => props.config, () => {
    loadEmbedData()
}, { deep: true })
</script>

<template>
    <div class="component-wrapper">
        <div v-if="error" class="error">{{ error }}</div>
        <PowerBIEmbed v-if="embedData" :embedUrl="embedData.embedUrl" :accessToken="embedData.accessToken"
            embedType="dashboard" :reportId="props.config.dashboardId as string"
            :filterPaneEnabled="props.config.filterPaneEnabled as boolean"
            :background="props.config.background as any" />
        <div v-else-if="!error && (props.config.workspaceId && props.config.dashboardId)" class="loading">Loading
            dashboard...</div>
        <div v-else-if="!error" class="placeholder">Please configure the dashboard.</div>
    </div>
</template>

<style scoped>
.component-wrapper {
    width: 100%;
    height: 100%;
    display: flex;
    flex-direction: column;
}

.error {
    color: #da1e28;
    padding: 1rem;
}

.loading,
.placeholder {
    padding: 1rem;
    color: #525252;
    display: flex;
    align-items: center;
    justify-content: center;
    height: 100%;
    background-color: #f4f4f4;
}
</style>
