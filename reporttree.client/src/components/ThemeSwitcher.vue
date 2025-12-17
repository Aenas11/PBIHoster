<script setup lang="ts">
import '@carbon/web-components/es/components/overflow-menu/index.js';
import { useThemeStore } from '@/stores/theme'
import type { ThemeType } from '@/stores/theme'
import { ColorSwitch20 } from '@carbon/icons-vue'

const themeStore = useThemeStore()

function changeTheme(event: CustomEvent) {
    const value = event.detail.item.getAttribute('value');
    if (!value) return;

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
    <cds-overflow-menu class="theme-switcher" flipped>
        <ColorSwitch20 slot="icon" class="theme-switcher-icon" aria-label="Theme" />
        <cds-overflow-menu-body @cds-overflow-menu-item-select="changeTheme">
            <cds-overflow-menu-item value="white">White</cds-overflow-menu-item>
            <cds-overflow-menu-item value="g10">Gray 10</cds-overflow-menu-item>
            <cds-overflow-menu-item value="g90">Gray 90</cds-overflow-menu-item>
            <cds-overflow-menu-item value="g100">Gray 100</cds-overflow-menu-item>

            <template v-if="themeStore.customThemes.length > 0">
                <cds-overflow-menu-item disabled divider>Custom Themes</cds-overflow-menu-item>
                <cds-overflow-menu-item v-for="theme in themeStore.customThemes" :key="theme.id" :value="theme.name">
                    {{ theme.name }}
                </cds-overflow-menu-item>
            </template>
        </cds-overflow-menu-body>
    </cds-overflow-menu>
</template>


<style scoped>
.theme-switcher {
    height: 3rem;
    width: 3rem;
}

/* Target the button inside cds-overflow-menu */
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
