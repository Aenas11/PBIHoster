<script setup lang="ts">
import { computed, ref, onMounted, onUnmounted, watch } from 'vue'
import PowerBIReportEmbed from '../PowerBIReportEmbed.vue'
import { powerBIService } from '../../services/powerbi.service'
import { useToastStore } from '../../stores/toast'
import { useThemeStore } from '../../stores/theme'
import { reportClientError } from '../../services/monitoring'
import type { DashboardComponentProps } from '../../types/components'
import type { EmbedTokenResponseDto } from '../../types/powerbi'
import '@carbon/web-components/es/components/button/index.js'

const props = defineProps<DashboardComponentProps>()
const toast = useToastStore()
const themeStore = useThemeStore()

const embedData = ref<EmbedTokenResponseDto | null>(null)
const runtimeError = ref<string | null>(null)
const refreshTimer = ref<number | null>(null)
const retryTimer = ref<number | null>(null)
const isRefreshing = ref(false)
const retryCount = ref(0)
const maxRetries = 3

const syncWithAppTheme = computed(() => (props.config.syncWithAppTheme as boolean) ?? false)

const mappedContrastMode = computed(() => {
    if (!syncWithAppTheme.value) return undefined
    return themeStore.currentTheme === 'g90' || themeStore.currentTheme === 'g100'
        ? 'HighContrastBlack'
        : undefined
})

const mappedBackground = computed<'Default' | 'Transparent'>(() => {
    if (!syncWithAppTheme.value) {
        return (props.config.background as 'Default' | 'Transparent') ?? 'Transparent'
    }
    return 'Transparent'
})

const configError = computed(() => {
    const { workspaceId, reportId } = props.config
    if (!workspaceId || !reportId) {
        return 'Please configure both Workspace and Report IDs.'
    }
    return null
})

const showLoading = computed(() => !runtimeError.value && !configError.value && !embedData.value)

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
    const { workspaceId, reportId, enableRLS, rlsRoles, syncWithAppTheme } = props.config
    if (!workspaceId || !reportId) {
        embedData.value = null
        runtimeError.value = null
        clearRetryTimer()
        return
    }

    try {
        embedData.value = await powerBIService.getReportEmbedToken(
            workspaceId as string,
            reportId as string,
            undefined, // pageId - not needed for component embed
            enableRLS as boolean | undefined,
            rlsRoles as string[] | undefined,
            syncWithAppTheme as boolean | undefined
        )
        runtimeError.value = null
        retryCount.value = 0 // Reset on success
        clearRetryTimer()

        if (embedData.value?.expiration) {
            scheduleRefresh(embedData.value.expiration)
        }

        // Show success toast on retry
        if (isRetry) {
            toast.success('Report Loaded', 'Successfully reconnected to Power BI')
        }
    } catch (e: unknown) {
        const friendlyMessage = getUserFriendlyError(e)
        runtimeError.value = friendlyMessage
        embedData.value = null
        console.error('Power BI Report Error:', e)

        // Retry logic for transient errors
        if (retryCount.value < maxRetries && !isRetry && shouldRetry(e)) {
            retryCount.value++
            toast.warning('Retrying...', `Attempt ${retryCount.value} of ${maxRetries}`, 3000)
            clearRetryTimer()
            retryTimer.value = window.setTimeout(() => loadEmbedData(true), 2000 * retryCount.value)
        } else {
            toast.error('Report Load Failed', friendlyMessage, 7000)
            await reportClientError({
                message: friendlyMessage,
                info: 'PowerBIReportComponent token/embed failed after retries',
                context: {
                    componentId: props.id,
                    workspaceId: workspaceId as string,
                    reportId: reportId as string,
                    retryCount: retryCount.value
                }
            })
            retryCount.value = 0
        }
    }
}

const retryNow = () => {
    retryCount.value = 0
    clearRetryTimer()
    loadEmbedData(true)
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
            if (!runtimeError.value) {
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
    clearRetryTimer()
})

watch(() => props.config, () => {
    loadEmbedData()
}, { deep: true })
</script>

<template>
    <div class="component-wrapper">
        <div v-if="configError" class="state-message state-message--config">{{ configError }}</div>
        <div v-else-if="runtimeError" class="state-message state-message--error">
            <span>{{ runtimeError }}</span>
            <cds-button size="sm" kind="tertiary" @click="retryNow">Retry</cds-button>
        </div>
        <PowerBIReportEmbed v-if="embedData" :embedUrl="embedData.embedUrl" :accessToken="embedData.accessToken"
            :reportId="props.config.reportId as string" :filterPaneEnabled="props.config.filterPaneEnabled as boolean"
            :navContentPaneEnabled="props.config.navContentPaneEnabled as boolean"
            :background="mappedBackground" :contrastMode="mappedContrastMode" />
        <div v-else-if="showLoading" class="state-message state-message--loading">Loading report...
        </div>
    </div>
</template>

<style scoped>
.component-wrapper {
    width: 100%;
    height: 100%;
    display: flex;
    flex-direction: column;
}

.state-message {
    padding: 1rem;
    color: #525252;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.75rem;
    height: 100%;
    background-color: #f4f4f4;
}

.state-message--error {
    color: #da1e28;
    background: #fff0f1;
    justify-content: space-between;
}

.state-message--config {
    color: #8a3c00;
    background: #fff8e1;
}
</style>
