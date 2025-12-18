<script setup lang="ts">
import '@carbon/web-components/es/components/select/index.js';
import '@carbon/web-components/es/components/select/select-item.js';
import type { ComponentConfigProps } from '../../types/components'
import { ref, onMounted, watch } from 'vue'
import { powerBIService } from '../../services/powerbi.service'
import type { WorkspaceDto, ReportDto } from '../../types/powerbi'

const props = defineProps<ComponentConfigProps>()
const emit = defineEmits<{
    'update:modelValue': [value: Record<string, unknown>]
}>()

const workspaces = ref<WorkspaceDto[]>([])
const reports = ref<ReportDto[]>([])
const selectedWorkspace = ref(props.modelValue.workspaceId as string || '')
const selectedReport = ref(props.modelValue.reportId as string || '')
const selectedViewOption = ref(props.modelValue.viewOptions as string || 'FitToPage')

const loadWorkspaces = async () => {
    try {
        workspaces.value = await powerBIService.getWorkspaces()
    } catch (e) {
        console.error("Failed to load workspaces", e)
    }
}

const loadReports = async (workspaceId: string) => {
    if (!workspaceId) {
        reports.value = []
        return
    }
    try {
        reports.value = await powerBIService.getReports(workspaceId)
    } catch (e) {
        console.error("Failed to load reports", e)
    }
}

onMounted(async () => {
    await loadWorkspaces()
    if (selectedWorkspace.value) {
        await loadReports(selectedWorkspace.value)
    }
})

watch(selectedWorkspace, async (newVal) => {
    // Only reset report if workspace changed and it's not the initial load
    if (newVal !== props.modelValue.workspaceId) {
        selectedReport.value = ''
    }
    await loadReports(newVal)
    updateConfig()
})

watch([selectedReport, selectedViewOption], () => {
    updateConfig()
})

const updateConfig = () => {
    emit('update:modelValue', {
        ...props.modelValue,
        workspaceId: selectedWorkspace.value,
        reportId: selectedReport.value,
        viewOptions: selectedViewOption.value
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

        <cds-select label-text="Report" :value="selectedReport"
            @cds-select-selected="selectedReport = $event.detail.value" :disabled="!selectedWorkspace">
            <cds-select-item value="" disabled>Select Report</cds-select-item>
            <cds-select-item v-for="rep in reports" :key="rep.id" :value="rep.id">{{ rep.name }}</cds-select-item>
        </cds-select>

        <cds-select label-text="View Options" :value="selectedViewOption"
            @cds-select-selected="selectedViewOption = $event.detail.value">
            <cds-select-item value="FitToPage">Fit To Page</cds-select-item>
            <cds-select-item value="FitToWidth">Fit To Width</cds-select-item>
            <cds-select-item value="ActualSize">Actual Size</cds-select-item>
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
