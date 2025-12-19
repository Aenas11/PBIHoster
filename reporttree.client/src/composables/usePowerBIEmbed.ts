import { ref, onMounted, onUnmounted, type Ref } from 'vue'
import * as pbi from 'powerbi-client'

export interface PowerBIEmbedOptions {
    embedContainer: Ref<HTMLElement | null>
    embedUrl: string
    accessToken: string
    onConfigReady: (service: pbi.service.Service) => pbi.IEmbedConfiguration
}

export function usePowerBIEmbed(options: PowerBIEmbedOptions) {
    const isLoading = ref(true)
    const error = ref<string | null>(null)
    let embed: pbi.Embed | undefined
    let powerbi: pbi.service.Service

    const getUserFriendlyError = (detail: { message?: string, detailedMessage?: string, errorCode?: string }): string => {
        if (detail.errorCode) {
            switch (detail.errorCode) {
                case 'TokenExpired':
                    return 'Access token expired. Refreshing...'
                case 'NotFound':
                    return 'Content not found. Please verify it exists and you have access.'
                case 'Forbidden':
                    return 'Access denied. You may not have permission to view this content.'
                default:
                    return detail.message || detail.detailedMessage || 'Error loading Power BI content'
            }
        }
        return detail.message || detail.detailedMessage || 'Error loading Power BI content'
    }

    const setupEventHandlers = (embedInstance: pbi.Embed) => {
        embedInstance.on('loaded', () => {
            console.log('Power BI Loaded')
            isLoading.value = false
        })

        embedInstance.on('rendered', () => {
            console.log('Power BI Rendered')
            isLoading.value = false
        })

        embedInstance.on('error', (event: pbi.service.ICustomEvent<unknown>) => {
            console.error('Power BI Error', event.detail)
            const detail = event.detail as { message?: string, detailedMessage?: string, errorCode?: string }
            error.value = getUserFriendlyError(detail)
            isLoading.value = false
        })
    }

    const embedContent = () => {
        if (!options.embedContainer.value || !options.embedUrl || !options.accessToken) {
            return
        }

        isLoading.value = true
        error.value = null

        // Reset existing embed
        if (powerbi && options.embedContainer.value) {
            powerbi.reset(options.embedContainer.value)
        }

        try {
            // Get configuration from callback
            const config = options.onConfigReady(powerbi)

            // Bootstrap first for performance
            powerbi.bootstrap(options.embedContainer.value, { type: config.type })

            // Embed the content
            embed = powerbi.embed(options.embedContainer.value, config)

            // Setup event handlers
            setupEventHandlers(embed)
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

    const cleanup = () => {
        if (embed) {
            embed.off('loaded')
            embed.off('rendered')
            embed.off('error')
            embed = undefined
        }
        if (powerbi && options.embedContainer.value) {
            powerbi.reset(options.embedContainer.value)
        }
    }

    onMounted(() => {
        // Initialize Power BI service
        powerbi = new pbi.service.Service(
            pbi.factories.hpmFactory,
            pbi.factories.wpmpFactory,
            pbi.factories.routerFactory
        )

        embedContent()
    })

    onUnmounted(() => {
        cleanup()
    })

    return {
        isLoading,
        error,
        embedContent,
        getEmbedInstance: () => embed,
        getPowerBIService: () => powerbi
    }
}
