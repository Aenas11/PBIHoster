<script setup lang="ts">
import { useThemeStore } from '@/stores/theme'
import type { ThemeType } from '@/stores/theme'
import { ColorSwitch20 } from '@carbon/icons-vue'

const themeStore = useThemeStore()

function changeTheme(value: string) {
    // Check if it's a custom theme
    const customTheme = themeStore.customThemes.find(t => t.name === value)
    if (customTheme) {
        themeStore.setCustomTheme(customTheme)
    } else {
        themeStore.setTheme(value as ThemeType)
    }
}
</script>

<template>
    <cv-overflow-menu :flip-menu="true" class="theme-switcher" tip-position="bottom" tip-alignment="end"
        aria-label="Theme Switcher">
        <template v-slot:trigger>
            <!-- <Switcher20 class="theme-switcher-icon" /> -->
            <!-- <ColorPalette class="theme-switcher-icon" /> -->
            <ColorSwitch20 class="theme-switcher-icon" aria-label="Theme" />
        </template>

        <cv-overflow-menu-item @click="changeTheme('white')">
            White
        </cv-overflow-menu-item>
        <cv-overflow-menu-item @click="changeTheme('g10')">
            Gray 10
        </cv-overflow-menu-item>
        <cv-overflow-menu-item @click="changeTheme('g90')">
            Gray 90
        </cv-overflow-menu-item>
        <cv-overflow-menu-item @click="changeTheme('g100')">
            Gray 100
        </cv-overflow-menu-item>

        <template v-if="themeStore.customThemes.length > 0">
            <cv-overflow-menu-item disabled class="theme-divider">
                Custom Themes
            </cv-overflow-menu-item>
            <cv-overflow-menu-item v-for="theme in themeStore.customThemes" :key="theme.id"
                @click="changeTheme(theme.name)">
                {{ theme.name }}
            </cv-overflow-menu-item>
        </template>
    </cv-overflow-menu>
</template>

<style scoped>
.theme-switcher {
    height: 3rem;
    width: 3rem;
}

/* Target the button inside cv-overflow-menu */
:deep(button) {
    width: 100%;
    height: 100%;
    min-height: 3rem;
    padding: 0;
    border: 0;
    border-radius: 0;
    box-shadow: none;
    background-color: transparent;
    display: flex;
    align-items: center;
    justify-content: center;
}

:deep(button:hover) {
    background-color: #353535;
}

:deep(button:focus) {
    outline: 2px solid #ffffff;
    outline-offset: -2px;
}

:deep(.theme-switcher-icon) {
    fill: #ffffff;
}

.theme-divider {
    font-weight: 600;
    opacity: 0.6;
    cursor: default;
}
</style>
