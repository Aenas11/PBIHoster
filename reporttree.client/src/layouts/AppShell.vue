<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useLayout } from '../composables/useLayout'
import AppHeader from '../components/AppHeader.vue'
import TheSideMenu from '../components/TheSideMenu.vue'
import ToolsPanel from '../components/ToolsPanel.vue'

const router = useRouter()
const auth = useAuthStore()
const layout = useLayout()

const isToolsPanelExpanded = ref(false)

function handleLogout() {
  auth.logout()
  router.push('/login')
}

function toggleToolsPanel() {
  isToolsPanelExpanded.value = !isToolsPanelExpanded.value
}

const contentStyle = computed(() => ({
  marginLeft: layout.contentMarginLeft.value,
  marginTop: '3rem',
  transition: 'margin-left 0.11s cubic-bezier(0.2, 0, 1, 0.9)',
  padding: '0',
  minHeight: 'calc(100vh - 3rem)' // Account for header height
}))
</script>

<template>
  <div class="app-shell">
    <AppHeader :is-side-nav-expanded="layout.isSideNavExpanded.value" :is-tools-panel-expanded="isToolsPanelExpanded"
      @toggle-menu="layout.toggleSideNav" @toggle-tools-panel="toggleToolsPanel" @logout="handleLogout">
      <template #right-panels>
        <ToolsPanel :expanded="isToolsPanelExpanded" />
      </template>
    </AppHeader>

    <TheSideMenu :fixed="layout.isSideNavFixed.value" v-model:expanded="layout.isSideNavExpanded.value"
      class="app-side-nav" />

    <div id="main-content" class="cds--content" :style="contentStyle">
      <router-view v-slot="{ Component }">
        <keep-alive :max="5">
          <component :is="Component" />
        </keep-alive>
      </router-view>
    </div>
  </div>
</template>

<style scoped>
.app-shell {
  flex: 1;
  display: flex;
  flex-direction: column;
  position: relative;
  min-height: calc(100vh - 3rem);
  /* Account for header */
}

:deep(.app-side-nav) {
  position: fixed !important;
  top: 3rem !important;
  height: calc(100vh - 3rem) !important;
  max-height: calc(100vh - 3rem) !important;
  z-index: 900;
  overflow-y: auto;
}

#main-content {
  flex: 1;
  overflow-y: auto;
  position: relative;
  padding-bottom: 2rem;
}
</style>
