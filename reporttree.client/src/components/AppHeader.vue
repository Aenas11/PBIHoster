<script setup lang="ts">
import { Logout20, Switcher20 } from '@carbon/icons-vue'
import ThemeSwitcher from './ThemeSwitcher.vue'

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
  <cv-header aria-label="ReportTree">
    <cv-header-menu-button aria-label="Header menu" aria-controls="side-nav" @click="emit('toggleMenu')"
      :active="isSideNavExpanded" />
    <cv-header-name href="#" prefix="">ReportTree</cv-header-name>

    <template v-slot:header-global>
      <!-- Theme Switcher -->
      <ThemeSwitcher />

      <cv-header-global-action aria-label="Tools" label="Tools" :active="isToolsPanelExpanded"
        @click="emit('toggleToolsPanel')" tip-position="bottom" tip-alignment="end">
        <Switcher20 />
      </cv-header-global-action>
      <cv-header-global-action aria-label="Logout" label="Logout" @click="emit('logout')" tip-position="bottom"
        tip-alignment="end">
        <Logout20 />
      </cv-header-global-action>


    </template>

    <template v-slot:right-panels>
      <slot name="right-panels"></slot>
    </template>
  </cv-header>
</template>
