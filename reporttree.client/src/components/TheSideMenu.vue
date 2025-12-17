<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useLayout } from '../composables/useLayout'
import { Add20 } from '@carbon/icons-vue'
import '@carbon/web-components/es/components/ui-shell/index.js';

defineProps<{
  fixed: boolean
}>()

const expanded = defineModel<boolean>('expanded')

const auth = useAuthStore()
const layout = useLayout()
const router = useRouter()

const isAdmin = computed(() => auth.roles.includes('Admin'))
const canCreate = computed(() => auth.roles.includes('Admin') || auth.roles.includes('Editor'))

function handleMobileNavigation() {
  // Use the composable's method
  layout.closeSideNavOnMobile()
}

function navigateTo(path: string, event: Event) {
  event.preventDefault();
  router.push(path);
  handleMobileNavigation();
}

function createPage() {
  // TODO: Implement create page logic
  console.log('Create page clicked')
}
</script>

<template>
  <cds-side-nav id="side-nav" :fixed="fixed" :expanded="expanded" aria-label="Side navigation"
    class="side-nav-container">
    <cds-side-nav-items>
      <cds-side-nav-link href="/" @click="navigateTo('/', $event)">Dashboard</cds-side-nav-link>
      <cds-side-nav-link href="/reports" @click="navigateTo('/reports', $event)">Reports</cds-side-nav-link>
      <cds-side-nav-link v-if="isAdmin" href="/admin" @click="navigateTo('/admin', $event)">
        Admin
      </cds-side-nav-link>
    </cds-side-nav-items>

    <div class="side-nav-spacer"></div>

    <cds-side-nav-items v-if="canCreate">
      <cds-side-nav-divider></cds-side-nav-divider>
      <cds-side-nav-link href="javascript:void(0)" @click="createPage">
        <Add20 slot="title-icon" />
        Create Page
      </cds-side-nav-link>
    </cds-side-nav-items>
  </cds-side-nav>
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
