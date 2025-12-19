<script setup lang="ts">
import '@carbon/web-components/es/components/select/index.js';
import '@carbon/web-components/es/components/select/select-item.js';
import '@carbon/web-components/es/components/toggle/index.js';
import '@carbon/web-components/es/components/accordion/index.js';
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

// Basic Display Settings
const selectedPageView = ref(props.modelValue.pageView as string || 'fitToWidth')
const selectedBackground = ref(props.modelValue.background as string || 'Transparent')

// Locale Settings
const selectedLocale = ref(props.modelValue.locale as string || 'en-US')

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

watch([selectedDashboard, selectedPageView, selectedBackground, selectedLocale], () => {
    updateConfig()
})

const updateConfig = () => {
    emit('update:modelValue', {
        ...props.modelValue,
        workspaceId: selectedWorkspace.value,
        dashboardId: selectedDashboard.value,
        pageView: selectedPageView.value,
        background: selectedBackground.value,
        locale: selectedLocale.value
    })
}
</script>

<template>
    <div class="config-container">
        <cds-accordion>
            <!-- Basic Configuration -->
            <cds-accordion-item title-text="Basic Configuration" open>
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
                    @cds-select-selected="(e: any) => { selectedPageView = e.detail.value }">
                    <cds-select-item value="fitToWidth">Fit to Width (Responsive)</cds-select-item>
                    <cds-select-item value="oneColumn">One Column (Mobile-Friendly)</cds-select-item>
                    <cds-select-item value="actualSize">Actual Size (Full Size)</cds-select-item>
                </cds-select>

                <cds-select label-text="Background" :value="selectedBackground"
                    @cds-select-selected="(e: any) => { selectedBackground = e.detail.value }">
                    <cds-select-item value="Transparent">Transparent</cds-select-item>
                    <cds-select-item value="Default">Default (White)</cds-select-item>
                </cds-select>
            </cds-accordion-item>

            <!-- Localization -->
            <cds-accordion-item title-text="Localization">
                <cds-select label-text="Language & Locale" :value="selectedLocale"
                    @cds-select-selected="(e: any) => { selectedLocale = e.detail.value }">
                    <cds-select-item value="en-US">English (United States)</cds-select-item>
                    <cds-select-item value="en-GB">English (United Kingdom)</cds-select-item>
                    <cds-select-item value="es-ES">Spanish (Spain)</cds-select-item>
                    <cds-select-item value="es-MX">Spanish (Mexico)</cds-select-item>
                    <cds-select-item value="fr-FR">French (France)</cds-select-item>
                    <cds-select-item value="de-DE">German (Germany)</cds-select-item>
                    <cds-select-item value="it-IT">Italian (Italy)</cds-select-item>
                    <cds-select-item value="pt-BR">Portuguese (Brazil)</cds-select-item>
                    <cds-select-item value="pt-PT">Portuguese (Portugal)</cds-select-item>
                    <cds-select-item value="ja-JP">Japanese (Japan)</cds-select-item>
                    <cds-select-item value="zh-CN">Chinese (Simplified)</cds-select-item>
                    <cds-select-item value="zh-TW">Chinese (Traditional)</cds-select-item>
                    <cds-select-item value="ko-KR">Korean (Korea)</cds-select-item>
                    <cds-select-item value="ru-RU">Russian (Russia)</cds-select-item>
                    <cds-select-item value="nl-NL">Dutch (Netherlands)</cds-select-item>
                    <cds-select-item value="pl-PL">Polish (Poland)</cds-select-item>
                    <cds-select-item value="tr-TR">Turkish (Türkiye)</cds-select-item>
                    <cds-select-item value="ar-SA">Arabic (Saudi Arabia)</cds-select-item>
                    <cds-select-item value="sv-SE">Swedish (Sweden)</cds-select-item>
                    <cds-select-item value="da-DK">Danish (Denmark)</cds-select-item>
                </cds-select>
            </cds-accordion-item>
        </cds-accordion>
    </div>
</template>

<style scoped>
.config-container {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.section-title {
    font-weight: 600;
    margin-top: 1rem;
    margin-bottom: 0.5rem;
    color: #161616;
}

cds-accordion-item {
    margin-bottom: 0.5rem;
}

cds-toggle {
    margin-bottom: 1rem;
}

cds-select {
    margin-bottom: 1rem;
}
</style>
