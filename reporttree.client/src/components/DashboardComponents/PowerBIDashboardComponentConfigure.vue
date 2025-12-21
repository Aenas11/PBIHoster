<script setup lang="ts">
import '@carbon/web-components/es/components/select/index.js';
import '@carbon/web-components/es/components/select/select-item.js';
import type { ComponentConfigProps } from '../../types/components'
import { ref, onMounted, watch } from 'vue'
import { powerBIService } from '../../services/powerbi.service'
import type { WorkspaceDto, DashboardDto } from '../../types/powerbi'

const props = defineProps<ComponentConfigProps>()
const emit = defineEmits<{
    'update:modelValue': [value: Record<string, unknown>]
}>()

const workspaces = ref<WorkspaceDto[]>([])
const dashboards = ref<DashboardDto[]>([])
const selectedWorkspace = ref((props.modelValue.workspaceId as string) || '')
const selectedDashboard = ref((props.modelValue.dashboardId as string) || '')

// Dashboard Display Settings - pageView values per https://learn.microsoft.com/en-us/javascript/api/overview/powerbi/embed-dashboard
// Valid values: 'fitToWidth', 'oneColumn', 'actualSize'
const selectedPageView = ref((props.modelValue.pageView as string | undefined) ?? 'fitToWidth')

const loadWorkspaces = async () => {
    try {
        workspaces.value = await powerBIService.getWorkspaces()
    } catch (e) {
        console.error("Failed to load workspaces", e)
    }
}

const loadDashboards = async (workspaceId: string) => {
    if (!workspaceId) {
        dashboards.value = []
        return
    }
    try {
        dashboards.value = await powerBIService.getDashboards(workspaceId)
    } catch (e) {
        console.error("Failed to load dashboards", e)
    }
}

onMounted(async () => {
    await loadWorkspaces()
    if (selectedWorkspace.value) {
        await loadDashboards(selectedWorkspace.value)
    }
})

watch(selectedWorkspace, async (newVal) => {
    if (newVal !== props.modelValue.workspaceId) {
        selectedDashboard.value = ''
    }
    await loadDashboards(newVal)
    updateConfig()
})

watch([selectedDashboard, selectedPageView], () => {
    updateConfig()
})

const updateConfig = () => {
    emit('update:modelValue', {
        ...props.modelValue,
        workspaceId: selectedWorkspace.value,
        dashboardId: selectedDashboard.value,
        pageView: selectedPageView.value
    })
}
</script>

<template>
    <div class="config-container">
        <cds-select label-text="Workspace" :value="selectedWorkspace"
            @cds-select-selected="selectedWorkspace = $event.detail.value">
            <cds-select-item value="" disabled>Select Workspace</cds-select-item>
            <cds-select-item v-for="ws in workspaces" :key="ws.id" :value="ws.id">{{ ws.name
            }}</cds-select-item>
        </cds-select>

        <cds-select label-text="Dashboard" :value="selectedDashboard"
            @cds-select-selected="selectedDashboard = $event.detail.value" :disabled="!selectedWorkspace">
            <cds-select-item value="" disabled>Select Dashboard</cds-select-item>
            <cds-select-item v-for="db in dashboards" :key="db.id" :value="db.id">{{ db.name
            }}</cds-select-item>
        </cds-select>

        <cds-select label-text="Display Mode" :value="selectedPageView"
            @cds-select-selected="selectedPageView = $event.detail.value">
            <cds-select-item value="fitToWidth" :selected="selectedPageView === 'fitToWidth'">Fit to Width
                (Responsive)</cds-select-item>
            <cds-select-item value="oneColumn" :selected="selectedPageView === 'oneColumn'">One Column
                (Mobile-Friendly)</cds-select-item>
            <cds-select-item value="actualSize" :selected="selectedPageView === 'actualSize'">Actual Size (Full
                Size)</cds-select-item>
        </cds-select>
    </div>
</template>

<style scoped>
.config-container {
    display: flex;
    flex-direction: column;
    padding-left: 0.25rem;
}

.section-title {
    font-weight: 600;
    margin-top: 1rem;
    margin-bottom: 0.5rem;
    color: #161616;
}
</style>
