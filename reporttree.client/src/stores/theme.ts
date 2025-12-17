import { defineStore } from 'pinia'
import { ref } from 'vue'
import { themes as builtInThemes } from '../config/themes'

export interface CustomTheme {
    id: string
    name: string
    tokens: Record<string, string>
    isCustom: boolean
    organizationId?: string
    createdBy?: string
    createdAt?: string
    updatedAt?: string
}

export type ThemeType = 'white' | 'g10' | 'g90' | 'g100' | 'custom'

export const useThemeStore = defineStore('theme', () => {
    const currentTheme = ref<ThemeType>('white')
    const customTheme = ref<CustomTheme | null>(null)
    const customThemes = ref<CustomTheme[]>([])

    // Load theme from localStorage on init
    const savedTheme = localStorage.getItem('theme') as ThemeType | null
    if (savedTheme) {
        currentTheme.value = savedTheme
    }

    const savedCustomTheme = localStorage.getItem('customTheme')
    if (savedCustomTheme) {
        try {
            customTheme.value = JSON.parse(savedCustomTheme)
        } catch (e) {
            console.error('Failed to parse custom theme', e)
        }
    }

    // Helper to convert camelCase to kebab-case
    function toKebabCase(str: string) {
        return str.replace(/([a-z0-9]|(?=[A-Z]))([A-Z])/g, '$1-$2').toLowerCase();
    }

    // Apply theme to document
    function applyTheme(theme: ThemeType, customTokens?: Record<string, string>) {
        const root = document.documentElement

        // Remove all existing theme classes
        root.classList.remove('cds--white', 'cds--g10', 'cds--g90', 'cds--g100')

        if (theme === 'custom' && customTokens) {
            // Apply custom theme tokens as CSS variables
            Object.entries(customTokens).forEach(([key, value]) => {
                // Custom tokens might already be kebab-case or camelCase, handle both if needed
                // For now assuming custom tokens follow the same convention as built-in if they come from the same editor
                const cssKey = toKebabCase(key);
                root.style.setProperty(`--cds-${cssKey}`, value)
            })
            root.classList.add('cds--white') // Use white as base for custom themes
        } else if (theme !== 'custom') {
            // Apply built-in theme
            const themeTokens = builtInThemes[theme]
            Object.entries(themeTokens).forEach(([key, value]) => {
                if (typeof value === 'string') {
                    const cssKey = toKebabCase(key);
                    root.style.setProperty(`--cds-${cssKey}`, value)
                }
            })
            root.classList.add(`cds--${theme}`)
        }
    }

    // Set theme
    function setTheme(theme: ThemeType) {
        currentTheme.value = theme
        localStorage.setItem('theme', theme)

        if (theme === 'custom' && customTheme.value) {
            applyTheme(theme, customTheme.value.tokens)
        } else {
            applyTheme(theme)
        }
    }

    // Set custom theme
    function setCustomTheme(theme: CustomTheme) {
        customTheme.value = theme
        currentTheme.value = 'custom'
        localStorage.setItem('customTheme', JSON.stringify(theme))
        localStorage.setItem('theme', 'custom')
        applyTheme('custom', theme.tokens)
    }

    // Load custom themes for the organization
    async function loadCustomThemes() {
        try {
            const response = await fetch('/api/themes', {
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                }
            })

            if (response.ok) {
                customThemes.value = await response.json()
            }
        } catch (error) {
            console.error('Failed to load custom themes', error)
        }
    }

    // Save custom theme to backend
    async function saveCustomTheme(theme: Omit<CustomTheme, 'id'>) {
        try {
            const response = await fetch('/api/themes', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                },
                body: JSON.stringify(theme)
            })

            if (response.ok) {
                const savedTheme = await response.json()
                customThemes.value.push(savedTheme)
                return savedTheme
            }
        } catch (error) {
            console.error('Failed to save custom theme', error)
            throw error
        }
    }

    // Delete custom theme
    async function deleteCustomTheme(themeId: string) {
        try {
            const response = await fetch(`/api/themes/${themeId}`, {
                method: 'DELETE',
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                }
            })

            if (response.ok) {
                customThemes.value = customThemes.value.filter(t => t.id !== themeId)
                if (customTheme.value?.id === themeId) {
                    customTheme.value = null
                    setTheme('white')
                }
            }
        } catch (error) {
            console.error('Failed to delete custom theme', error)
            throw error
        }
    }

    // Initialize theme on app start
    function initTheme() {
        if (currentTheme.value === 'custom' && customTheme.value) {
            applyTheme('custom', customTheme.value.tokens)
        } else {
            applyTheme(currentTheme.value)
        }
    }

    return {
        currentTheme,
        customTheme,
        customThemes,
        setTheme,
        setCustomTheme,
        loadCustomThemes,
        saveCustomTheme,
        deleteCustomTheme,
        initTheme
    }
})
