<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import AppShell from './layouts/AppShell.vue'
import LoginLayout from './layouts/LoginLayout.vue'
import TheFooter from './components/TheFooter.vue'

const route = useRoute()

const currentLayout = computed(() => {
  return route.name === 'login' ? LoginLayout : AppShell
})
</script>

<template>
  <div class="app-wrapper">
    <component :is="currentLayout" />
    <TheFooter />
  </div>
</template>

<style scoped>
.app-wrapper {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}
</style>
