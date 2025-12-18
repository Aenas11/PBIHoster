<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import PowerBIEmbed from '../PowerBIEmbed.vue'
import { powerBIService } from '../../services/powerbi.service'
import type { DashboardComponentProps } from '../../types/components'
import type { EmbedTokenResponseDto } from '../../types/powerbi'

const props = defineProps<DashboardComponentProps>()

const embedData = ref<EmbedTokenResponseDto | null>(null)
const error = ref<string | null>(null)

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
    } catch (e: any) {
        error.value = "Failed to load dashboard token."
        console.error(e)
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
            :viewOptions="props.config.viewOptions as any" />
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
