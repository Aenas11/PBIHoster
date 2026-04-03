<script setup lang="ts">
import '@carbon/web-components/es/components/select/index.js';
import '@carbon/web-components/es/components/select/select-item.js';
import '@carbon/web-components/es/components/toggle/index.js';
import '@carbon/web-components/es/components/text-input/index.js';
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
const filterPaneEnabled = ref(props.modelValue.filterPaneEnabled as boolean ?? false)
const navContentPaneEnabled = ref(props.modelValue.navContentPaneEnabled as boolean ?? false)
const selectedBackground = ref(props.modelValue.background as string || 'Transparent')
const syncWithAppTheme = ref(props.modelValue.syncWithAppTheme as boolean ?? false)
const enableRLS = ref(props.modelValue.enableRLS as boolean ?? false)
const rlsRolesInput = ref((props.modelValue.rlsRoles as string[] || []).join(', '))

const hydrateFromModel = (model: Record<string, unknown>) => {
    selectedWorkspace.value = (model.workspaceId as string) || ''
    selectedReport.value = (model.reportId as string) || ''
    filterPaneEnabled.value = (model.filterPaneEnabled as boolean) ?? false
    navContentPaneEnabled.value = (model.navContentPaneEnabled as boolean) ?? false
    selectedBackground.value = (model.background as string) || 'Transparent'
    syncWithAppTheme.value = (model.syncWithAppTheme as boolean) ?? false
    enableRLS.value = (model.enableRLS as boolean) ?? false
    rlsRolesInput.value = ((model.rlsRoles as string[]) || []).join(', ')
}

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

watch([selectedReport, filterPaneEnabled, navContentPaneEnabled, selectedBackground, syncWithAppTheme, enableRLS, rlsRolesInput], () => {
    updateConfig()
})

watch(
    () => props.modelValue,
    async (newValue) => {
        hydrateFromModel(newValue)
        if (selectedWorkspace.value) {
            await loadReports(selectedWorkspace.value)
        } else {
            reports.value = []
        }
    },
    { deep: true, immediate: true }
)

const updateConfig = () => {
    // Parse comma-separated RLS roles
    const rlsRoles = rlsRolesInput.value
        .split(',')
        .map(role => role.trim())
        .filter(role => role.length > 0)

    emit('update:modelValue', {
        ...props.modelValue,
        workspaceId: selectedWorkspace.value,
        reportId: selectedReport.value,
        filterPaneEnabled: filterPaneEnabled.value,
        navContentPaneEnabled: navContentPaneEnabled.value,
        background: selectedBackground.value,
        syncWithAppTheme: syncWithAppTheme.value,
        enableRLS: enableRLS.value,
        rlsRoles: rlsRoles.length > 0 ? rlsRoles : undefined
    })
}

const onToggleChanged = (e: CustomEvent<{ checked?: boolean; value?: boolean }>) => {
    const value = e.detail?.checked ?? e.detail?.value
    return value ?? false
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

        <cds-toggle label-text="Show Filter Pane" :checked="filterPaneEnabled"
            @cds-toggle-changed="(e: CustomEvent<{ checked?: boolean; value?: boolean }>) => { filterPaneEnabled = onToggleChanged(e) }">
            <span slot="label-text">Show Filter Pane</span>
        </cds-toggle>

        <cds-toggle label-text="Show Page Navigation" :checked="navContentPaneEnabled"
            @cds-toggle-changed="(e: CustomEvent<{ checked?: boolean; value?: boolean }>) => { navContentPaneEnabled = onToggleChanged(e) }">
            <span slot="label-text">Show Page Navigation</span>
        </cds-toggle>

        <cds-select label-text="Background" :value="selectedBackground"
            @cds-select-selected="(e: any) => { selectedBackground = e.detail.value }">
            <cds-select-item value="Transparent">Transparent</cds-select-item>
            <cds-select-item value="Default">Default (White)</cds-select-item>
        </cds-select>

        <cds-toggle label-text="Sync with app theme" :checked="syncWithAppTheme"
            @cds-toggle-changed="(e: CustomEvent<{ checked?: boolean; value?: boolean }>) => { syncWithAppTheme = onToggleChanged(e) }">
            <span slot="label-text"
                title="Safe mode only adjusts embed chrome (background/contrast). It does not recolor Power BI visuals unless the report itself has a matching theme.">
                Sync embed appearance with app theme (safe mode)
            </span>
        </cds-toggle>

        <div class="rls-section">
            <h4 class="section-title">Row-Level Security (RLS)</h4>
            <cds-toggle label-text="Enable Row-Level Security" :checked="enableRLS"
                @cds-toggle-changed="(e: CustomEvent<{ checked?: boolean; value?: boolean }>) => { enableRLS = onToggleChanged(e) }">
                <span slot="label-text">Enable Row-Level Security</span>
            </cds-toggle>

            <cds-text-input v-if="enableRLS" label="RLS Roles (comma-separated)" :value="rlsRolesInput"
                @input="(e: any) => { rlsRolesInput = e.target.value }"
                placeholder="e.g., SalesRegion, Manager, Finance"
                helper-text="Enter Power BI RLS role names. The current user's identity will be passed to these roles.">
            </cds-text-input>
        </div>
    </div>
</template>

<style scoped>
.config-container {
    display: flex;
    flex-direction: column;
    padding-left: 0.25rem;
}

.rls-section {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    padding-top: 1rem;
    border-top: 1px solid #e0e0e0;
}

.section-title {
    margin: 0;
    font-size: 0.875rem;
    font-weight: 600;
    color: #161616;
}
</style>
