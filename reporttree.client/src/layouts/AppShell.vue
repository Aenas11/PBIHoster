<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useEditModeStore } from '../stores/editMode'
import { useLayout } from '../composables/useLayout'
import AppHeader from '../components/AppHeader.vue'
import TheSideMenu from '../components/TheSideMenu.vue'
import ToolsPanel from '../components/ToolsPanel.vue'
import TheFooter from '../components/TheFooter.vue'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()
const editModeStore = useEditModeStore()
const layout = useLayout()

const isToolsPanelExpanded = ref(false)

function handleLogout() {
  editModeStore.forceExitEditMode()
  auth.logout()
  router.push('/login')
}

function toggleToolsPanel() {
  isToolsPanelExpanded.value = !isToolsPanelExpanded.value
}

// Auto-close tools panel when exiting edit mode
watch(() => editModeStore.isEditMode, (newValue) => {
  if (!newValue && isToolsPanelExpanded.value) {
    isToolsPanelExpanded.value = false
  }
})

// Exit edit mode when navigating away from page views
watch(() => route.path, (newPath) => {
  if (!newPath.startsWith('/page/') && editModeStore.isEditMode) {
    editModeStore.exitEditMode()
  }
})

const contentStyle = computed(() => ({
  marginLeft: layout.contentMarginLeft.value,
  transition: 'margin-left 0.11s cubic-bezier(0.2, 0, 1, 0.9)',
  padding: '0'
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
      <div class="content-wrapper">
        <router-view v-slot="{ Component }">
          <keep-alive :max="5">
            <component :is="Component" />
          </keep-alive>
        </router-view>
      </div>
    </div>
    <TheFooter />
  </div>
</template>

<style scoped>
.app-shell {
  flex: 1;
  display: flex;
  flex-direction: column;
  position: relative;
  min-height: 100vh;
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
  display: flex;
  flex-direction: column;
}

.content-wrapper {
  flex: 1;
  padding-top: 3rem;
}
</style>
