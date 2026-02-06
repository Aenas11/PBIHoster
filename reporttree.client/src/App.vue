<script setup lang="ts">
import { computed, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useThemeStore } from './stores/theme'
import { useAuthStore } from './stores/auth'
import { useStaticSettingsStore } from './stores/staticSettings'
import AppShell from './layouts/AppShell.vue'
import LoginLayout from './layouts/LoginLayout.vue'
import ToastNotification from './components/ToastNotification.vue'
import GlobalErrorBoundary from './components/GlobalErrorBoundary.vue'

const route = useRoute()
const themeStore = useThemeStore()
const authStore = useAuthStore()
const staticSettingsStore = useStaticSettingsStore()

const currentLayout = computed(() => {
  return route.name === 'login' ? LoginLayout : AppShell
})

onMounted(async () => {
  // Initialize theme
  themeStore.initTheme()

  // Load custom themes if user is authenticated
  if (authStore.isAuthenticated) {
    await themeStore.loadCustomThemes()
  }

  if (!staticSettingsStore.isLoaded) {
    await staticSettingsStore.load()
  }
})

function applyBranding(appName: string, faviconUrl: string) {
  if (appName) {
    document.title = appName
  }

  if (faviconUrl) {
    let favicon = document.querySelector<HTMLLinkElement>('link[rel="icon"]')
    if (!favicon) {
      favicon = document.createElement('link')
      favicon.rel = 'icon'
      document.head.appendChild(favicon)
    }
    favicon.href = faviconUrl
  }
}

watch(
  () => [staticSettingsStore.appName, staticSettingsStore.faviconUrl],
  ([appName, faviconUrl]) => {
    applyBranding(appName || '', faviconUrl || '')
  },
  { immediate: true }
)
</script>

<template>
  <div class="app-wrapper">
    <GlobalErrorBoundary>
      <component :is="currentLayout" />
    </GlobalErrorBoundary>
    <ToastNotification />
  </div>
</template>

<style>
html,
body {
  height: 100%;
  margin: 0;
  padding: 0;
}

#app {
  height: 100%;
}
</style>

<style scoped>
.app-wrapper {
  display: flex;
  flex-direction: column;
  height: 100%;
  width: 100%;
}
</style>
