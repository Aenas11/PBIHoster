<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue'
import PowerBIEmbed from '../PowerBIEmbed.vue'
import { powerBIService } from '../../services/powerbi.service'
import { useToastStore } from '../../stores/toast'
import type { DashboardComponentProps } from '../../types/components'
import type { EmbedTokenResponseDto } from '../../types/powerbi'

const props = defineProps<DashboardComponentProps>()
const toast = useToastStore()

const embedData = ref<EmbedTokenResponseDto | null>(null)
const error = ref<string | null>(null)
const refreshTimer = ref<number | null>(null)
const isRefreshing = ref(false)
const retryCount = ref(0)
const maxRetries = 3

const getUserFriendlyError = (e: unknown): string => {
    if (e instanceof Error) {
        if (e.message.includes('401') || e.message.includes('Unauthorized')) {
            return 'Power BI authentication failed. Please check your credentials.'
        }
        if (e.message.includes('404') || e.message.includes('Not Found')) {
            return 'Report not found. It may have been deleted or you may not have access.'
        }
        if (e.message.includes('403') || e.message.includes('Forbidden')) {
            return 'You do not have permission to access this report.'
        }
        if (e.message.includes('Network') || e.message.includes('fetch')) {
            return 'Network error. Please check your connection and try again.'
        }
        if (e.message.includes('RLS') || e.message.includes('role')) {
            return 'Row-level security configuration error. Please check your RLS roles.'
        }
    }
    return 'Failed to load report. Please try again later.'
}

const loadEmbedData = async (isRetry = false) => {
    const { workspaceId, reportId, enableRLS, rlsRoles } = props.config
    if (!workspaceId || !reportId) {
        // Don't show error if just initialized empty
        if (workspaceId === undefined && reportId === undefined) return

        error.value = "Workspace and Report not configured."
        return
    }

    try {
        embedData.value = await powerBIService.getReportEmbedToken(
            workspaceId as string,
            reportId as string,
            undefined, // pageId - not needed for component embed
            enableRLS as boolean | undefined,
            rlsRoles as string[] | undefined
        )
        error.value = null
        retryCount.value = 0 // Reset on success

        if (embedData.value?.expiration) {
            scheduleRefresh(embedData.value.expiration)
        }

        // Show success toast on retry
        if (isRetry) {
            toast.success('Report Loaded', 'Successfully reconnected to Power BI')
        }
    } catch (e: unknown) {
        const friendlyMessage = getUserFriendlyError(e)
        error.value = friendlyMessage
        console.error('Power BI Report Error:', e)

        // Retry logic for transient errors
        if (retryCount.value < maxRetries && !isRetry) {
            retryCount.value++
            toast.warning('Retrying...', `Attempt ${retryCount.value} of ${maxRetries}`, 3000)
            setTimeout(() => loadEmbedData(true), 2000 * retryCount.value)
        } else {
            toast.error('Report Load Failed', friendlyMessage, 7000)
            retryCount.value = 0
        }
    }
}

const scheduleRefresh = (expiration: string) => {
    if (refreshTimer.value) clearTimeout(refreshTimer.value)

    const expireTime = new Date(expiration).getTime()
    const now = new Date().getTime()
    const timeUntilExpire = expireTime - now
    // Refresh 5 minutes before expiration
    const refreshTime = timeUntilExpire - (5 * 60 * 1000)

    if (refreshTime > 0) {
        refreshTimer.value = window.setTimeout(async () => {
            console.log("Refreshing Power BI token...")
            isRefreshing.value = true
            await loadEmbedData()
            isRefreshing.value = false
            if (!error.value) {
                toast.info('Token Refreshed', 'Power BI access token has been renewed', 3000)
            }
        }, refreshTime)
    }
}

onMounted(() => {
    loadEmbedData()
})

onUnmounted(() => {
    if (refreshTimer.value) clearTimeout(refreshTimer.value)
})

watch(() => props.config, () => {
    loadEmbedData()
}, { deep: true })
</script>

<template>
    <div class="component-wrapper">
        <div v-if="error" class="error">{{ error }}</div>
        <PowerBIEmbed v-if="embedData" :embedUrl="embedData.embedUrl" :accessToken="embedData.accessToken"
            embedType="report" :reportId="props.config.reportId as string"
            :filterPaneEnabled="props.config.filterPaneEnabled as boolean"
            :navContentPaneEnabled="props.config.navContentPaneEnabled as boolean"
            :background="props.config.background as any" />
        <div v-else-if="!error && (props.config.workspaceId && props.config.reportId)" class="loading">Loading report...
        </div>
        <div v-else-if="!error" class="placeholder">Please configure the report.</div>
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
