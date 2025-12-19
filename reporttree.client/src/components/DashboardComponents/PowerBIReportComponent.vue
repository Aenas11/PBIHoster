<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue'
import PowerBIEmbed from '../PowerBIEmbed.vue'
import { powerBIService } from '../../services/powerbi.service'
import type { DashboardComponentProps } from '../../types/components'
import type { EmbedTokenResponseDto } from '../../types/powerbi'

const props = defineProps<DashboardComponentProps>()

const embedData = ref<EmbedTokenResponseDto | null>(null)
const error = ref<string | null>(null)
const refreshTimer = ref<number | null>(null)

const loadEmbedData = async () => {
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

        if (embedData.value?.expiration) {
            scheduleRefresh(embedData.value.expiration)
        }
    } catch (e: any) {
        error.value = "Failed to load report token."
        console.error(e)
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
        refreshTimer.value = window.setTimeout(() => {
            console.log("Refreshing Power BI token...")
            loadEmbedData()
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
