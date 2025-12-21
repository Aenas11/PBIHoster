<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useThemeStore } from './stores/theme'
import { useAuthStore } from './stores/auth'
import AppShell from './layouts/AppShell.vue'
import LoginLayout from './layouts/LoginLayout.vue'
import ToastNotification from './components/ToastNotification.vue'
import GlobalErrorBoundary from './components/GlobalErrorBoundary.vue'

const route = useRoute()
const themeStore = useThemeStore()
const authStore = useAuthStore()

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
})
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
