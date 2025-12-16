<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '../stores/auth'

defineProps<{
  fixed: boolean
}>()

const expanded = defineModel<boolean>('expanded')

const auth = useAuthStore()
const isAdmin = computed(() => auth.roles.includes('Admin'))

function handleMobileNavigation() {
  // Close side nav on mobile after navigation
  if (window.innerWidth <= 1056) {
    expanded.value = false
  }
}
</script>

<template>
  <cv-side-nav id="side-nav" :fixed="fixed" v-model:expanded="expanded" aria-label="Side navigation">
    <cv-side-nav-items>
      <cv-side-nav-link to="/" @click="handleMobileNavigation">Dashboard</cv-side-nav-link>
      <cv-side-nav-link to="/reports" @click="handleMobileNavigation">Reports</cv-side-nav-link>
      <cv-side-nav-link 
        v-if="isAdmin"
        to="/admin"
        @click="handleMobileNavigation"
      >
        Admin
      </cv-side-nav-link>
    </cv-side-nav-items>
  </cv-side-nav>
</template>
