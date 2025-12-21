<script setup lang="ts">
import '@carbon/web-components/es/components/select/index.js';
import '@carbon/web-components/es/components/select/select-item.js';
import '@carbon/web-components/es/components/toggle/index.js';
import '@carbon/web-components/es/components/text-input/index.js';
import type { ComponentConfigProps } from '../../types/components'
import { ref, onMounted, watch } from 'vue'
import { powerBIService } from '../../services/powerbi.service'
import type { WorkspaceDto } from '../../types/powerbi'

const props = defineProps<ComponentConfigProps>()
const emit = defineEmits<{
    'update:modelValue': [value: Record<string, unknown>]
}>()

const workspaces = ref<WorkspaceDto[]>([])
const selectedWorkspace = ref(props.modelValue.workspaceId as string || '')
const enableRLS = ref(props.modelValue.enableRLS as boolean ?? false)
const rlsRolesInput = ref((props.modelValue.rlsRoles as string[] || []).join(', '))

const loadWorkspaces = async () => {
    try {
        workspaces.value = await powerBIService.getWorkspaces()
    } catch (e) {
        console.error("Failed to load workspaces", e)
    }
}

onMounted(async () => {
    await loadWorkspaces()
})

watch([selectedWorkspace, enableRLS, rlsRolesInput], () => {
    updateConfig()
})

const updateConfig = () => {
    // Parse comma-separated RLS roles
    const rlsRoles = rlsRolesInput.value
        .split(',')
        .map(role => role.trim())
        .filter(role => role.length > 0)

    emit('update:modelValue', {
        ...props.modelValue,
        workspaceId: selectedWorkspace.value,
        enableRLS: enableRLS.value,
        rlsRoles: rlsRoles.length > 0 ? rlsRoles : undefined
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

        <div class="info-text">
            All reports from this workspace will be displayed with tabs.
        </div>

        <div class="rls-section">
            <h4 class="section-title">Row-Level Security (RLS)</h4>
            <cds-toggle label-text="Enable Row-Level Security" :checked="enableRLS"
                @cds-toggle-changed="(e: any) => { enableRLS = e.detail.checked }">
                <span slot="label-text">Enable Row-Level Security</span>
            </cds-toggle>

            <cds-text-input v-if="enableRLS" label="RLS Roles (comma-separated)" :value="rlsRolesInput"
                @input="(e: any) => { rlsRolesInput = e.target.value }"
                placeholder="e.g., SalesRegion, Manager, Finance"
                helper-text="Enter Power BI RLS role names. Applied to all reports in this workspace.">
            </cds-text-input>
        </div>
    </div>
</template>

<style scoped>
.config-container {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.info-text {
    font-size: 0.875rem;
    color: #525252;
    padding: 0.5rem 0;
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
