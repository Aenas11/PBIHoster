<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useThemeStore } from './stores/theme'
import { useAuthStore } from './stores/auth'
import AppShell from './layouts/AppShell.vue'
import LoginLayout from './layouts/LoginLayout.vue'
import TheFooter from './components/TheFooter.vue'

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
    <component :is="currentLayout" />
    <TheFooter />
  </div>
</template>

<style scoped></style>
