<script setup lang="ts">
import { computed, ref, onMounted, useAttrs } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useEditModeStore } from '../stores/editMode'
import { usePagesStore } from '../stores/pages'
import { useLayout } from '../composables/useLayout'
import {
  Add20, Dashboard20, Document20, UserAdmin20, Pin20, PinFilled20,
  Edit20, Folder20, ChartBar20, Table20, SettingsEdit20, Close20
} from '@carbon/icons-vue'
import '@carbon/web-components/es/components/ui-shell/index.js';
import '@carbon/web-components/es/components/icon-button/index.js';
import '@carbon/web-components/es/components/button/index.js';
import PageModal from './PageModal.vue'
import type { Page } from '../types/page'

defineProps<{
  fixed: boolean
}>()

const expanded = defineModel<boolean>('expanded')

const auth = useAuthStore()
const editModeStore = useEditModeStore()
const pagesStore = usePagesStore()
const layout = useLayout()
const router = useRouter()

const attrs = useAttrs()

const isAdmin = computed(() => auth.roles.includes('Admin'))

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
  editModeStore.toggleEditMode()
  if (editModeStore.isEditMode && !expanded.value) {
    expanded.value = true // Expand when entering edit mode
  }
}

function handleMobileNavigation() {
  layout.closeSideNavOnMobile()
}

function navigateTo(path: string, event: Event) {
  event.preventDefault();
  if (editModeStore.isEditMode) return; // Disable navigation in edit mode
  router.push(path);
  handleMobileNavigation();
}

function handleItemClick(page: Page, event: Event) {
  if (editModeStore.isEditMode) {
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
  <cds-side-nav v-bind="attrs" id="side-nav" :fixed="fixed" :expanded="expanded" :collapse-mode="collapseMode"
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
          @click="handleItemClick(page, $event)" :class="{ 'edit-mode-item': editModeStore.isEditMode }">
          <component :is="getIcon(page.icon)" slot="title-icon" />
          {{ page.title }}
          <span v-if="editModeStore.isEditMode" class="edit-badge">✎</span>
        </cds-side-nav-link>

        <!-- Submenu Node -->
        <cds-side-nav-menu v-else :title="page.title">
          <component :is="getIcon(page.icon)" slot="title-icon" />

          <!-- Edit Parent Link (Only in Edit Mode) -->
          <cds-side-nav-link v-if="editModeStore.isEditMode" href="javascript:void(0)" @click="openEditModal(page)"
            class="edit-folder-link">
            <SettingsEdit20 slot="title-icon" />
            Edit Folder Properties
          </cds-side-nav-link>

          <cds-side-nav-link v-for="child in page.children" :key="child.id" :href="`/page/${child.id}`"
            @click="handleItemClick(child, $event)" :class="{ 'edit-mode-item': editModeStore.isEditMode }">
            <component :is="getIcon(child.icon)" slot="title-icon" />
            {{ child.title }}
            <span v-if="editModeStore.isEditMode" class="edit-badge">✎</span>
          </cds-side-nav-link>

          <!-- Add Child Button in Edit Mode -->
          <cds-side-nav-link v-if="editModeStore.isEditMode" href="javascript:void(0)" @click="openCreateModal(page.id)"
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

    <div class="side-nav-footer">
      <div v-if="editModeStore.canEdit" class="side-nav-actions">
        <cds-button kind="secondary" size="md" :has-icon-only="!expanded"
          :aria-label="!expanded ? (editModeStore.isEditMode ? 'Exit Edit Mode' : 'Edit Pages') : ''"
          @click="toggleEditMode" class="action-button">
          <span v-if="expanded">{{ editModeStore.isEditMode ? 'Exit Edit Mode' : 'Edit Pages' }}</span>
          <span v-if="!expanded" slot="tooltip-content">{{ editModeStore.isEditMode ? 'Exit Edit Mode' : 'Edit Pages'
            }}</span>
          <component :is="editModeStore.isEditMode ? Close20 : Edit20" slot="icon" />
        </cds-button>

        <cds-button v-if="editModeStore.isEditMode" kind="primary" size="md" :has-icon-only="!expanded"
          :aria-label="!expanded ? 'New Top Level Page' : ''" @click="openCreateModal(null)" class="action-button">
          <span v-if="expanded">New Top Level Page</span>
          <span v-if="!expanded" slot="tooltip-content">New Top Level Page</span>
          <Add20 slot="icon" />
        </cds-button>
      </div>

      <cds-side-nav-items>
        <cds-side-nav-divider></cds-side-nav-divider>
        <cds-side-nav-link href="javascript:void(0)" @click="togglePin">
          <component :is="expanded ? PinFilled20 : Pin20" slot="title-icon" />
          {{ expanded ? 'Unpin Menu' : 'Pin Menu' }}
        </cds-side-nav-link>
      </cds-side-nav-items>
    </div>
  </cds-side-nav>

  <PageModal :open="isModalOpen" :page="selectedPage" :parent-id="parentIdForNewPage" @close="isModalOpen = false"
    @create-child="handleCreateChild" />
</template>

<style scoped>
.side-nav-footer {
  position: absolute;
  bottom: 4rem;
  width: 100%;
}

.side-nav-actions {
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
  gap: 0.5rem;
  padding: 0.5rem;
}

.action-button {
  width: 100%;
  max-width: 100%;
}

.edit-folder-link,
.add-child-link {
  --cds-link-primary: var(--cds-text-secondary, #525252);
  background-color: rgba(141, 141, 141, 0.08);
  border-left: 3px solid var(--cds-border-strong, #8d8d8d);
  font-style: italic;
}

.edit-badge {
  margin-left: auto;
  font-size: 0.75rem;
  color: var(--cds-text-secondary, #525252);
}
</style>
