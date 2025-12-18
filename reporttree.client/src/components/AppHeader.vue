<script setup lang="ts">
import '@carbon/web-components/es/components/ui-shell/index.js';
import { Logout20, Switcher20 } from '@carbon/icons-vue'
// import ThemeSwitcher from './ThemeSwitcher.vue'

defineProps<{
  isSideNavExpanded: boolean
  isToolsPanelExpanded: boolean
}>()

const emit = defineEmits<{
  toggleMenu: []
  toggleToolsPanel: []
  logout: []
}>()
</script>

<template>
  <cds-header aria-label="ReportTree">
    <cds-skip-to-content></cds-skip-to-content>
    <cds-header-name href="javascript:void 0" prefix="Report">Tree</cds-header-name>
    <cds-header-menu-button button-label-active="Close menu" button-label-inactive="Open menu"
      @click="emit('toggleMenu')" :active="isSideNavExpanded" />

    <div class="cds--header__global">
      <!-- Theme Switcher -->
      <!-- <ThemeSwitcher /> -->

      <cds-header-global-action aria-label="App Switcher" tooltip-text="App Switcher" tooltip-alignment="right"
        @click="emit('toggleToolsPanel')">
        <Switcher20 slot="icon" />
      </cds-header-global-action>

      <cds-header-global-action aria-label="Logout" tooltip-text="Logout" @click="emit('logout')"
        tooltip-position="bottom" tooltip-alignment="end">
        <Logout20 slot="icon" class="header-icon" />
      </cds-header-global-action>

    </div>

    <slot name="right-panels"></slot>
  </cds-header>
</template>

<style scoped>
.cds--header__global {
  display: flex;
  height: 100%;
  align-items: center;
}

cds-header-global-action {
  flex-shrink: 0;
}
</style>
