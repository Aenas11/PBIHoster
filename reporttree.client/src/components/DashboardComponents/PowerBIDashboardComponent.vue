<script setup lang="ts">
import { computed, ref, onMounted, onUnmounted, watch } from 'vue'
import PowerBIDashboardEmbed from '../PowerBIDashboardEmbed.vue'
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
const retryCount = ref(0)
const maxRetries = 2
const retryTimer = ref<number | null>(null)

const syncWithAppTheme = computed(() => (props.config.syncWithAppTheme as boolean) ?? false)

const mappedBackground = computed<'Default' | 'Transparent'>(() => {
    if (!syncWithAppTheme.value) {
        return (props.config.background as 'Default' | 'Transparent') ?? 'Transparent'
    }

    return themeStore.currentTheme === 'g90' || themeStore.currentTheme === 'g100'
        ? 'Transparent'
        : 'Default'
})

const configError = computed(() => {
    const { workspaceId, dashboardId } = props.config
    if (!workspaceId || !dashboardId) {
        return 'Please configure both Workspace and Dashboard IDs.'
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

const loadEmbedData = async (isRetry = false) => {
    const { workspaceId, dashboardId, syncWithAppTheme } = props.config
    if (!workspaceId || !dashboardId) {
        embedData.value = null
        runtimeError.value = null
        clearRetryTimer()
        return
    }

    try {
        embedData.value = await powerBIService.getDashboardEmbedToken(
            workspaceId as string,
            dashboardId as string,
            undefined,
            syncWithAppTheme as boolean | undefined
        )
        runtimeError.value = null
        retryCount.value = 0
        clearRetryTimer()
        if (isRetry) {
            toast.success('Dashboard Loaded', 'Successfully reconnected to Power BI')
        }
    } catch (e: unknown) {
        const friendlyMessage = getUserFriendlyError(e)
        runtimeError.value = friendlyMessage
        embedData.value = null
        if (retryCount.value < maxRetries && !isRetry && shouldRetry(e)) {
            retryCount.value++
            clearRetryTimer()
            retryTimer.value = window.setTimeout(() => loadEmbedData(true), 1500 * retryCount.value)
        } else {
            toast.error('Dashboard Load Failed', friendlyMessage, 7000)
            await reportClientError({
                message: friendlyMessage,
                info: 'PowerBIDashboardComponent token/embed failed after retries',
                context: {
                    componentId: props.id,
                    workspaceId: workspaceId as string,
                    dashboardId: dashboardId as string,
                    retryCount: retryCount.value
                }
            })
            retryCount.value = 0
        }
        console.error('Power BI Dashboard Error:', e)
    }
}

const retryNow = () => {
    retryCount.value = 0
    clearRetryTimer()
    loadEmbedData(true)
}

onMounted(() => {
    loadEmbedData()
})

onUnmounted(() => {
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
        <PowerBIDashboardEmbed v-if="embedData" :embedUrl="embedData.embedUrl" :accessToken="embedData.accessToken"
            :dashboardId="props.config.dashboardId as string" :pageView="props.config.pageView as string"
            :locale="props.config.locale as string" :background="mappedBackground" />
        <div v-else-if="showLoading" class="state-message state-message--loading">Loading dashboard...</div>
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
