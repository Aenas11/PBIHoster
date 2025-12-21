<script setup lang="ts">
import '@carbon/web-components/es/components/ui-shell/index.js';
import { useRouter } from 'vue-router'
import { Logout20, Switcher20, User20 } from '@carbon/icons-vue'
import { useAuthStore } from '@/stores/auth'
import { useEditModeStore } from '@/stores/editMode'
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

function goToProfile() {
  router.push('/profile')
}

</script>

<template>
  <cds-header aria-label="ReportTree">
    <cds-skip-to-content></cds-skip-to-content>
    <cds-header-menu-button button-label-active="Close menu" button-label-inactive="Open menu"
      @click="emit('toggleMenu')" :active="isSideNavExpanded" />
    <cds-header-name href="/" prefix="Report">Tree</cds-header-name>


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
</style>
