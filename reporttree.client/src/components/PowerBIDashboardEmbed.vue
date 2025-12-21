<script setup lang="ts">
import { ref, watch } from 'vue'
import { models } from 'powerbi-client'
import '@carbon/web-components/es/components/loading/index.js'
import { usePowerBIEmbed } from '../composables/usePowerBIEmbed'

const props = defineProps<{
    embedUrl: string
    accessToken: string
    dashboardId: string
    pageView?: models.PageView | string
    locale?: string
    background?: 'Default' | 'Transparent'
}>()

const embedContainer = ref<HTMLElement | null>(null)

const { isLoading, error, embedContent } = usePowerBIEmbed({
    embedContainer,
    embedUrl: props.embedUrl,
    accessToken: props.accessToken,
    onConfigReady: () => {
        return {
            type: 'dashboard',
            tokenType: models.TokenType.Embed,
            accessToken: props.accessToken,
            embedUrl: props.embedUrl,
            id: props.dashboardId,
            pageView: typeof props.pageView === 'string' ? props.pageView as models.PageView : props.pageView,
            settings: {
                background: props.background === 'Transparent'
                    ? models.BackgroundType.Transparent
                    : models.BackgroundType.Default,
                localeSettings: props.locale ? {
                    language: props.locale
                } : undefined
            }
        }
    }
})

// Re-embed when URL or token changes
watch(() => [props.embedUrl, props.accessToken], () => {
    embedContent()
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
