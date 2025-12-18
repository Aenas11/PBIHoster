<script setup lang="ts">
import { computed, ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { usePagesStore } from '../stores/pages'
import { useLayout } from '../composables/useLayout'
import {
  Add20, Dashboard20, Document20, UserAdmin20, Pin20, PinFilled20,
  Edit20, Folder20, ChartBar20, Table20, SettingsEdit20
} from '@carbon/icons-vue'
import '@carbon/web-components/es/components/ui-shell/index.js';
import PageModal from './PageModal.vue'
import type { Page } from '../types/page'

defineProps<{
  fixed: boolean
}>()

const expanded = defineModel<boolean>('expanded')

const auth = useAuthStore()
const pagesStore = usePagesStore()
const layout = useLayout()
const router = useRouter()

const isAdmin = computed(() => auth.roles.includes('Admin'))
const canEdit = computed(() => auth.roles.includes('Admin') || auth.roles.includes('Editor'))

const isEditMode = ref(false)
const isModalOpen = ref(false)
const selectedPage = ref<Page | null>(null)
const parentIdForNewPage = ref<number | null>(null)

const collapseMode = computed(() => {
  if (!layout.isSideNavFixed.value) return 'responsive'
  return expanded.value ? 'fixed' : 'rail'
})

onMounted(() => {
  pagesStore.fetchPages()
})

function togglePin() {
  expanded.value = !expanded.value
}

function toggleEditMode() {
  isEditMode.value = !isEditMode.value
  if (isEditMode.value && !expanded.value) {
    expanded.value = true // Expand when entering edit mode
  }
}

function handleMobileNavigation() {
  layout.closeSideNavOnMobile()
}

function navigateTo(path: string, event: Event) {
  event.preventDefault();
  if (isEditMode.value) return; // Disable navigation in edit mode
  router.push(path);
  handleMobileNavigation();
}

function handleItemClick(page: Page, event: Event) {
  if (isEditMode.value) {
    event.preventDefault()
    openEditModal(page)
  } else {
    navigateTo(`/page/${page.id}`, event)
  }
}

function openCreateModal(parentId: number | null = null) {
  selectedPage.value = null
  parentIdForNewPage.value = parentId
  isModalOpen.value = true
}

function openEditModal(page: Page) {
  selectedPage.value = page
  parentIdForNewPage.value = null
  isModalOpen.value = true
}

function handleCreateChild(parentId: number) {
  isModalOpen.value = false
  // Use setTimeout to allow the modal to close before opening the new one
  setTimeout(() => {
    openCreateModal(parentId)
  }, 100)
}

// Icon mapping
const iconMap: Record<string, typeof Dashboard20> = {
  Dashboard20, Document20, Folder20, ChartBar20, Table20
}

function getIcon(iconName: string) {
  return iconMap[iconName] || Document20
}
</script>

<template>
  <cds-side-nav id="side-nav" :fixed="fixed" :expanded="expanded" :collapse-mode="collapseMode"
    aria-label="Side navigation" class="side-nav-container">
    <cds-side-nav-items>
      <!-- Static Items -->
      <cds-side-nav-link href="/" @click="navigateTo('/', $event)">
        <Dashboard20 slot="title-icon" />
        Dashboard
      </cds-side-nav-link>

      <!-- Dynamic Pages -->
      <template v-for="page in pagesStore.pages" :key="page.id">
        <!-- Leaf Node -->
        <cds-side-nav-link v-if="!page.children || page.children.length === 0" :href="`/page/${page.id}`"
          @click="handleItemClick(page, $event)" :class="{ 'edit-mode-item': isEditMode }">
          <component :is="getIcon(page.icon)" slot="title-icon" />
          {{ page.title }}
          <span v-if="isEditMode" class="edit-badge">✎</span>
        </cds-side-nav-link>

        <!-- Submenu Node -->
        <cds-side-nav-menu v-else :title="page.title">
          <component :is="getIcon(page.icon)" slot="title-icon" />

          <!-- Edit Parent Link (Only in Edit Mode) -->
          <cds-side-nav-link v-if="isEditMode" href="javascript:void(0)" @click="openEditModal(page)"
            class="edit-folder-link">
            <SettingsEdit20 slot="title-icon" />
            Edit Folder Properties
          </cds-side-nav-link>

          <cds-side-nav-link v-for="child in page.children" :key="child.id" :href="`/page/${child.id}`"
            @click="handleItemClick(child, $event)" :class="{ 'edit-mode-item': isEditMode }">
            <component :is="getIcon(child.icon)" slot="title-icon" />
            {{ child.title }}
            <span v-if="isEditMode" class="edit-badge">✎</span>
          </cds-side-nav-link>

          <!-- Add Child Button in Edit Mode -->
          <cds-side-nav-link v-if="isEditMode" href="javascript:void(0)" @click="openCreateModal(page.id)"
            class="add-child-link">
            <Add20 slot="title-icon" />
            Add Child Page
          </cds-side-nav-link>
        </cds-side-nav-menu>
      </template>

      <cds-side-nav-link v-if="isAdmin" href="/admin" @click="navigateTo('/admin', $event)">
        <UserAdmin20 slot="title-icon" />
        Admin
      </cds-side-nav-link>
    </cds-side-nav-items>

    <div class="side-nav-spacer"></div>

    <cds-side-nav-items>
      <cds-side-nav-divider></cds-side-nav-divider>

      <!-- Edit Mode Toggle -->
      <cds-side-nav-link v-if="canEdit" href="javascript:void(0)" @click="toggleEditMode" :active="isEditMode">
        <Edit20 slot="title-icon" />
        {{ isEditMode ? 'Exit Edit Mode' : 'Edit Pages' }}
      </cds-side-nav-link>

      <!-- Create Top Level Page (Only in Edit Mode) -->
      <cds-side-nav-link v-if="isEditMode" href="javascript:void(0)" @click="openCreateModal(null)">
        <Add20 slot="title-icon" />
        New Top Level Page
      </cds-side-nav-link>

      <cds-side-nav-link href="javascript:void(0)" @click="togglePin">
        <component :is="expanded ? PinFilled20 : Pin20" slot="title-icon" />
        {{ expanded ? 'Unpin Menu' : 'Pin Menu' }}
      </cds-side-nav-link>
    </cds-side-nav-items>
  </cds-side-nav>

  <PageModal :open="isModalOpen" :page="selectedPage" :parent-id="parentIdForNewPage" @close="isModalOpen = false"
    @create-child="handleCreateChild" />
</template>

<style scoped>
.side-nav-container {
  padding-bottom: 4rem;
  /* Ensure bottom items are not covered by footer */
}
</style>
