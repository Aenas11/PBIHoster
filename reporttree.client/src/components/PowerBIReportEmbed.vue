<script setup lang="ts">
import { ref, watch } from 'vue'
import { models, Report } from 'powerbi-client'
import '@carbon/web-components/es/components/loading/index.js'
import { usePowerBIEmbed } from '../composables/usePowerBIEmbed'

const props = defineProps<{
    embedUrl: string
    accessToken: string
    reportId: string
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

const { isLoading, error, embedContent, getEmbedInstance, getPowerBIService } = usePowerBIEmbed({
    embedContainer,
    embedUrl: props.embedUrl,
    accessToken: props.accessToken,
    onConfigReady: (service) => {
        // Determine page navigation position
        const pageNavPosition = props.pageNavPosition === 'Left'
            ? models.PageNavigationPosition.Left
            : models.PageNavigationPosition.Bottom

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
        }

        return {
            type: 'report',
            tokenType: models.TokenType.Embed,
            accessToken: props.accessToken,
            embedUrl: props.embedUrl,
            id: props.reportId,
            permissions: models.Permissions.Read,
            settings: {
                filterPaneEnabled: props.filterPaneEnabled ?? false,
                navContentPaneEnabled: props.navContentPaneEnabled ?? false,
                layoutType: props.mobileLayout ? models.LayoutType.MobilePortrait : models.LayoutType.Master,
                background: props.background === 'Transparent'
                    ? models.BackgroundType.Transparent
                    : models.BackgroundType.Default,
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
    }
})

// Re-embed when URL or token changes
watch(() => [props.embedUrl, props.accessToken], () => {
    embedContent()
})

// Switch layout when mobile layout changes
watch(() => props.mobileLayout, (newVal) => {
    const embed = getEmbedInstance()
    if (embed) {
        const report = embed as Report
        const layoutType = newVal ? models.LayoutType.MobilePortrait : models.LayoutType.Master
        report.switchLayout(layoutType)
    }
})
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
