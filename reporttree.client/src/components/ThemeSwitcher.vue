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

    <cds-overflow-menu class="theme-switcher" flipped aria-label="Theme switcher">
        <ColorSwitch20 />
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
.theme-switcher:deep(svg) {
    /* map color to var(--cds-link-primary, #0f62fe); */
    color: var(--cds-link-primary, #0f62fe);
}
</style>
