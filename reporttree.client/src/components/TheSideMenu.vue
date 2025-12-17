<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '../stores/auth'
import { useLayout } from '../composables/useLayout'
import { Add20 } from '@carbon/icons-vue'

defineProps<{
  fixed: boolean
}>()

const expanded = defineModel<boolean>('expanded')

const auth = useAuthStore()
const layout = useLayout()

const isAdmin = computed(() => auth.roles.includes('Admin'))
const canCreate = computed(() => auth.roles.includes('Admin') || auth.roles.includes('Editor'))

function handleMobileNavigation() {
  // Use the composable's method
  layout.closeSideNavOnMobile()
}

function createPage() {
  // TODO: Implement create page logic
  console.log('Create page clicked')
}
</script>

<template>
  <cv-side-nav id="side-nav" :fixed="fixed" v-model:expanded="expanded" aria-label="Side navigation" class="side-nav-container">
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
    
    <div class="side-nav-spacer"></div>

    <cv-side-nav-items v-if="canCreate">
      <cv-side-nav-divider />
      <cv-side-nav-link @click="createPage">
        <template #nav-icon>
          <Add20 />
        </template>
        Create Page
      </cv-side-nav-link>
    </cv-side-nav-items>
  </cv-side-nav>
</template>

<!-- <style scoped>
.side-nav-container :deep(.bx--side-nav__navigation) {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.side-nav-spacer {
  flex-grow: 1;
}
</style> -->
