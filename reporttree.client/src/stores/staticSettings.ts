import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useStaticSettingsStore = defineStore('staticSettings', () => {
    const homePageId = ref<string>('')
    const demoModeEnabled = ref(false)
    const commentsEnabled = ref(true)
    const enforceSensitivityLabels = ref(false)
    const version = ref('0.0.0')
    const appName = ref('ReportTree')
    const footerText = ref('')
    const footerLinkUrl = ref('')
    const footerLinkLabel = ref('')
    const logoUrl = ref('')
    const faviconUrl = ref('')
    const isLoaded = ref(false)

    const load = async () => {
        try {
            const response = await fetch('/api/settings/static')
            if (!response.ok) {
                throw new Error('Failed to load static settings')
            }
            const data = await response.json()
            homePageId.value = data.HomePageId || data.homePageId || ''
            version.value = data.Version || data.version || '0.0.0'
            appName.value = data.AppName || data.appName || 'ReportTree'
            footerText.value = data.FooterText || data.footerText || ''
            footerLinkUrl.value = data.FooterLinkUrl || data.footerLinkUrl || ''
            footerLinkLabel.value = data.FooterLinkLabel || data.footerLinkLabel || ''
            logoUrl.value = data.LogoUrl || data.logoUrl || ''
            faviconUrl.value = data.FaviconUrl || data.faviconUrl || ''

            const demoRaw = data.DemoModeEnabled ?? data.demoModeEnabled
            if (typeof demoRaw === 'string') {
                demoModeEnabled.value = demoRaw.toLowerCase() === 'true'
            } else {
                demoModeEnabled.value = !!demoRaw
            }

            const commentsRaw = data.CommentsEnabled ?? data.commentsEnabled
            if (typeof commentsRaw === 'string') {
                commentsEnabled.value = commentsRaw.toLowerCase() === 'true'
            } else if (commentsRaw === undefined || commentsRaw === null) {
                commentsEnabled.value = true
            } else {
                commentsEnabled.value = !!commentsRaw
            }

            const enforceRaw = data.EnforceSensitivityLabels ?? data.enforceSensitivityLabels
            if (typeof enforceRaw === 'string') {
                enforceSensitivityLabels.value = enforceRaw.toLowerCase() === 'true'
            } else {
                enforceSensitivityLabels.value = !!enforceRaw
            }
        } catch (error) {
            console.error('Static settings load failed', error)
        } finally {
            isLoaded.value = true
        }
    }

    return {
        homePageId,
        demoModeEnabled,
        commentsEnabled,
        enforceSensitivityLabels,
        version,
        appName,
        footerText,
        footerLinkUrl,
        footerLinkLabel,
        logoUrl,
        faviconUrl,
        isLoaded,
        load
    }
})
