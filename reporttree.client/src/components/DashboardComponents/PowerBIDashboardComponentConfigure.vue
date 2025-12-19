<script setup lang="ts">
import '@carbon/web-components/es/components/select/index.js';
import '@carbon/web-components/es/components/select/select-item.js';
import '@carbon/web-components/es/components/toggle/index.js';
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
const selectedWorkspace = ref(props.modelValue.workspaceId as string || '')
const selectedDashboard = ref(props.modelValue.dashboardId as string || '')
const filterPaneEnabled = ref(props.modelValue.filterPaneEnabled as boolean ?? false)
const selectedBackground = ref(props.modelValue.background as string || 'Transparent')

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

watch([selectedDashboard, filterPaneEnabled, selectedBackground], () => {
    updateConfig()
})

const updateConfig = () => {
    emit('update:modelValue', {
        ...props.modelValue,
        workspaceId: selectedWorkspace.value,
        dashboardId: selectedDashboard.value,
        filterPaneEnabled: filterPaneEnabled.value,
        background: selectedBackground.value
    })
}
</script>

<template>
    <div class="config-container">
        <cds-select label-text="Workspace" :value="selectedWorkspace"
            @cds-select-selected="selectedWorkspace = $event.detail.value">
            <cds-select-item value="" disabled>Select Workspace</cds-select-item>
            <cds-select-item v-for="ws in workspaces" :key="ws.id" :value="ws.id">{{ ws.name }}</cds-select-item>
        </cds-select>

        <cds-select label-text="Dashboard" :value="selectedDashboard"
            @cds-select-selected="selectedDashboard = $event.detail.value" :disabled="!selectedWorkspace">
            <cds-select-item value="" disabled>Select Dashboard</cds-select-item>
            <cds-select-item v-for="db in dashboards" :key="db.id" :value="db.id">{{ db.name }}</cds-select-item>
        </cds-select>

        <cds-toggle label-text="Show Filter Pane" :checked="filterPaneEnabled"
            @cds-toggle-changed="(e: any) => { filterPaneEnabled = e.detail.checked }">
            <span slot="label-text">Show Filter Pane</span>
        </cds-toggle>

        <cds-select label-text="Background" :value="selectedBackground"
            @cds-select-selected="(e: any) => { selectedBackground = e.detail.value }">
            <cds-select-item value="Transparent">Transparent</cds-select-item>
            <cds-select-item value="Default">Default (White)</cds-select-item>
        </cds-select>
    </div>
</template>

<style scoped>
.config-container {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}
</style>
