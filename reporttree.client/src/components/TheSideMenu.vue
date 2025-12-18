<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useLayout } from '../composables/useLayout'
import { Add20, Dashboard20, Document20, UserAdmin20, Equalizer20, Pin20, PinFilled20 } from '@carbon/icons-vue'
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

const collapseMode = computed(() => {
  // On mobile (not fixed), use responsive mode
  if (!layout.isSideNavFixed.value) return 'responsive'
  // On desktop/tablet, use fixed if expanded (pinned), rail if collapsed (unpinned)
  return expanded.value ? 'fixed' : 'rail'
})

function togglePin() {
  expanded.value = !expanded.value
}

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
  <cds-side-nav id="side-nav" :fixed="fixed" :expanded="expanded" :collapse-mode="collapseMode"
    aria-label="Side navigation" class="side-nav-container">
    <cds-side-nav-items>
      <cds-side-nav-link href="/" @click="navigateTo('/', $event)">
        <Dashboard20 slot="title-icon" />
        Dashboard
      </cds-side-nav-link>
      <cds-side-nav-link href="/reports" @click="navigateTo('/reports', $event)">
        <Document20 slot="title-icon" />
        Reports
      </cds-side-nav-link>
      <cds-side-nav-link v-if="isAdmin" href="/admin" @click="navigateTo('/admin', $event)">
        <UserAdmin20 slot="title-icon" />
        Admin
      </cds-side-nav-link>
      <!-- example of tree like structure -->
      <cds-side-nav-menu title="Management">
        <Equalizer20 slot="title-icon" />
        <cds-side-nav-link href="/users" @click="navigateTo('/users', $event)">
          Users
        </cds-side-nav-link>
        <cds-side-nav-link href="/settings" @click="navigateTo('/settings', $event)">
          Settings
        </cds-side-nav-link>
      </cds-side-nav-menu>
    </cds-side-nav-items>

    <div class="side-nav-spacer"></div>

    <cds-side-nav-items>
      <cds-side-nav-link v-if="canCreate" href="javascript:void(0)" @click="createPage">
        <Add20 slot="title-icon" />
        Create Page
      </cds-side-nav-link>

      <cds-side-nav-divider></cds-side-nav-divider>

      <cds-side-nav-link href="javascript:void(0)" @click="togglePin">
        <component :is="expanded ? PinFilled20 : Pin20" slot="title-icon" />
        {{ expanded ? 'Unpin Menu' : 'Pin Menu' }}
      </cds-side-nav-link>
    </cds-side-nav-items>
  </cds-side-nav>
</template>

<style scoped>
.side-nav-container {
  top: 3rem;
  /* Ensure it sits below the header */
}

.side-nav-container :deep(.cds--side-nav__navigation) {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.side-nav-spacer {
  flex-grow: 1;
}
</style>
