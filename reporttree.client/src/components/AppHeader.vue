<script setup lang="ts">
import '@carbon/web-components/es/components/ui-shell/index.js';
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { Logout20, Switcher20, User20 } from '@carbon/icons-vue'
import { useAuthStore } from '@/stores/auth'
import { useEditModeStore } from '@/stores/editMode'
import { useStaticSettingsStore } from '@/stores/staticSettings'
import '@carbon/web-components/es/components/tag/index.js'
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

const router = useRouter()
const authStore = useAuthStore()
const editModeStore = useEditModeStore()
const staticSettings = useStaticSettingsStore()
const appName = computed(() => staticSettings.appName || 'ReportTree')
const logoUrl = computed(() => staticSettings.logoUrl || '')

function goToProfile() {
  router.push('/profile')
}

</script>

<template>
  <cds-header :aria-label="appName">
    <cds-skip-to-content></cds-skip-to-content>
    <cds-header-menu-button button-label-active="Close menu" button-label-inactive="Open menu"
      @click="emit('toggleMenu')" :active="isSideNavExpanded" />
    <img v-if="logoUrl" :src="logoUrl" class="brand-logo" alt="" aria-hidden="true" />
    <cds-header-name href="/" prefix="">{{ appName }}</cds-header-name>
    <cds-tag v-if="staticSettings.demoModeEnabled" type="blue" title="Demo mode enabled" size="sm" class="demo-tag">
      Demo mode
    </cds-tag>


    <div class="cds--header__global" v-if="authStore.isAuthenticated">
      <!-- Theme Switcher -->
      <!-- <ThemeSwitcher /> -->

      <cds-header-global-action aria-label="App Switcher" tooltip-text="Tools Panel" tooltip-alignment="right"
        @click="emit('toggleToolsPanel')" :disabled="!editModeStore.isEditMode">
        <Switcher20 slot="icon" />
      </cds-header-global-action>

      <cds-header-global-action aria-label="User Profile" tooltip-text="User Profile" tooltip-alignment="right"
        @click="goToProfile">
        <User20 slot="icon" />
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

@media (min-width: 769px) {
  cds-header-menu-button {
    display: none;
  }
}

.demo-tag {
  margin-left: 0.5rem;
}

.brand-logo {
  height: 22px;
  width: auto;
  margin-left: 0.5rem;
  margin-right: 0.25rem;
}
</style>
