<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue'
import * as pbi from 'powerbi-client'
import { models } from 'powerbi-client'
import '@carbon/web-components/es/components/loading/index.js';

const props = defineProps<{
    embedUrl: string
    accessToken: string
    embedType: 'report' | 'dashboard'
    reportId: string
    mobileLayout?: boolean
    viewOptions?: 'FitToPage' | 'ActualSize' | 'FitToWidth'
}>()

const embedContainer = ref<HTMLElement | null>(null)
const isLoading = ref(true)
const error = ref<string | null>(null)
let embed: pbi.Embed | undefined
let powerbi: pbi.service.Service

onMounted(() => {
    // Initialize Power BI service
    powerbi = new pbi.service.Service(
        pbi.factories.hpmFactory,
        pbi.factories.wpmpFactory,
        pbi.factories.routerFactory
    )

    if (embedContainer.value) {
        embedReport()
    }
})

onUnmounted(() => {
    if (embed) {
        embed.off('loaded')
        embed.off('rendered')
        embed.off('error')
        embed = undefined
    }
    if (embedContainer.value) {
        powerbi.reset(embedContainer.value)
    }
})

watch(() => [props.embedUrl, props.accessToken], () => {
    embedReport()
})

watch(() => props.mobileLayout, (newVal) => {
    if (embed && props.embedType === 'report') {
        const report = embed as pbi.Report
        const layoutType = newVal ? models.LayoutType.MobilePortrait : models.LayoutType.Master
        report.switchLayout(layoutType)
    }
})

const embedReport = () => {
    if (!embedContainer.value || !props.embedUrl || !props.accessToken) return

    isLoading.value = true
    error.value = null

    // Reset existing
    powerbi.reset(embedContainer.value)

    const config: pbi.IEmbedConfiguration = {
        type: props.embedType,
        tokenType: models.TokenType.Embed,
        accessToken: props.accessToken,
        embedUrl: props.embedUrl,
        id: props.reportId,
        permissions: models.Permissions.Read,
        settings: {
            filterPaneEnabled: false,
            navContentPaneEnabled: false,
            layoutType: props.mobileLayout ? models.LayoutType.MobilePortrait : models.LayoutType.Master,
            background: models.BackgroundType.Transparent,
            panes: {
                filters: {
                    visible: false
                },
                pageNavigation: {
                    visible: false
                }
            }
        }
    }

    // Bootstrap first for performance
    powerbi.bootstrap(embedContainer.value, { type: props.embedType })

    // Embed
    try {
        embed = powerbi.embed(embedContainer.value, config)

        embed.on('loaded', () => {
            console.log('Power BI Loaded')
            isLoading.value = false
        })

        embed.on('rendered', () => {
            console.log('Power BI Rendered')
            isLoading.value = false
        })

        embed.on('error', (event: any) => {
            console.error('Power BI Error', event.detail)
            error.value = `Error loading Power BI content: ${event.detail?.message || 'Unknown error'}`
            isLoading.value = false
        })
    } catch (e: any) {
        console.error('Exception embedding Power BI', e)
        error.value = `Exception: ${e.message}`
        isLoading.value = false
    }
}
</script>

<template>
    <div class="embed-wrapper">
        <div v-if="isLoading" class="loading-overlay">
            <cds-loading active />
        </div>
        <div v-if="error" class="error-message">
            {{ error }}
        </div>
        <div ref="embedContainer" class="powerbi-container"></div>
    </div>
</template>

<style scoped>
.embed-wrapper {
    width: 100%;
    height: 100%;
    position: relative;
    display: flex;
    flex-direction: column;
}

.powerbi-container {
    width: 100%;
    height: 100%;
    flex: 1;
}

.loading-overlay {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    display: flex;
    justify-content: center;
    align-items: center;
    background-color: rgba(255, 255, 255, 0.7);
    z-index: 10;
}

.error-message {
    padding: 1rem;
    color: #da1e28;
    background-color: #fff0f1;
    border: 1px solid #ffb3b8;
}
</style>
