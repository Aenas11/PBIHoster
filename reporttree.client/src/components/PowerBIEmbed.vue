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
    pageView?: 'fitToWidth' | 'oneColumn' | 'actualSize'
    mobileLayout?: boolean
    filterPaneEnabled?: boolean
    filterPaneExpanded?: boolean
    navContentPaneEnabled?: boolean
    pageNavPosition?: string
    bookmarksVisible?: boolean
    actionBarVisible?: boolean
    statusBarVisible?: boolean
    locale?: string
    visualRenderedEvents?: boolean
    hideErrors?: boolean
    contrastMode?: string
    background?: 'Default' | 'Transparent'
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

    // Determine page navigation position
    const pageNavPosition = props.pageNavPosition === 'Left' ? models.PageNavigationPosition.Left : models.PageNavigationPosition.Bottom

    // Determine contrast mode
    let contrast = models.ContrastMode.None
    switch (props.contrastMode) {
        case 'HighContrast1':
            contrast = models.ContrastMode.HighContrast1
            break
        case 'HighContrast2':
            contrast = models.ContrastMode.HighContrast2
            break
        case 'HighContrastBlack':
            contrast = models.ContrastMode.HighContrastBlack
            break
        case 'HighContrastWhite':
            contrast = models.ContrastMode.HighContrastWhite
            break
        default:
            contrast = models.ContrastMode.None
    }


    const config: pbi.IEmbedConfiguration = {
        type: props.embedType,
        tokenType: models.TokenType.Embed,
        accessToken: props.accessToken,
        embedUrl: props.embedUrl,
        id: props.reportId,
        permissions: models.Permissions.Read,
        ...(props.embedType === 'dashboard' && { pageView: props.pageView }),
        settings: {
            filterPaneEnabled: props.filterPaneEnabled ?? false,
            navContentPaneEnabled: props.navContentPaneEnabled ?? false,
            layoutType: props.mobileLayout ? models.LayoutType.MobilePortrait : models.LayoutType.Master,
            background: props.background === 'Transparent' ? models.BackgroundType.Transparent : models.BackgroundType.Default,
            visualRenderedEvents: props.visualRenderedEvents ?? false,
            hideErrors: props.hideErrors ?? false,
            localeSettings: props.locale ? {
                language: props.locale
            } : undefined,
            ...(contrast !== models.ContrastMode.None && { contrastMode: contrast }),
            panes: {
                filters: {
                    visible: props.filterPaneEnabled ?? false,
                    expanded: props.filterPaneExpanded ?? false
                },
                pageNavigation: {
                    visible: props.navContentPaneEnabled ?? false,
                    position: pageNavPosition
                },
                bookmarks: {
                    visible: props.bookmarksVisible ?? false
                }
            },
            bars: {
                actionBar: {
                    visible: props.actionBarVisible ?? false
                },
                statusBar: {
                    visible: props.statusBarVisible ?? true
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

        embed.on('error', (event: pbi.service.ICustomEvent<unknown>) => {
            console.error('Power BI Error', event.detail)
            const detail = event.detail as { message?: string, detailedMessage?: string, errorCode?: string }
            let errorMsg = 'Error loading Power BI content'

            if (detail.errorCode) {
                switch (detail.errorCode) {
                    case 'TokenExpired':
                        errorMsg = 'Access token expired. Refreshing...'
                        break
                    case 'NotFound':
                        errorMsg = 'Content not found. Please verify the report/dashboard exists.'
                        break
                    case 'Forbidden':
                        errorMsg = 'Access denied. You may not have permission to view this content.'
                        break
                    default:
                        errorMsg = detail.message || detail.detailedMessage || errorMsg
                }
            } else {
                errorMsg = detail.message || detail.detailedMessage || errorMsg
            }

            error.value = errorMsg
            isLoading.value = false
        })
    } catch (e: unknown) {
        console.error('Exception embedding Power BI', e)
        let errorMsg = 'Failed to embed Power BI content'

        if (e instanceof Error) {
            if (e.message.includes('embed container')) {
                errorMsg = 'Invalid embed container. Please refresh the page.'
            } else if (e.message.includes('token')) {
                errorMsg = 'Invalid access token. Please contact your administrator.'
            } else {
                errorMsg = `Error: ${e.message}`
            }
        }

        error.value = errorMsg
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
