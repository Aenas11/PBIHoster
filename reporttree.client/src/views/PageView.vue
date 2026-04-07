<!-- page with dynamic layout -->
<script setup lang="ts">
import '@carbon/web-components/es/components/loading/index.js';
import '@carbon/web-components/es/components/button/index.js';
import '@carbon/web-components/es/components/modal/index.js';
import '@carbon/web-components/es/components/tabs/index.js';
import { GridLayout, GridItem } from 'grid-layout-plus'
import { useGridLayout } from '../composables/useGridLayout'
import { useComponentRegistry } from '../composables/useComponentRegistry'
import { useEditModeStore } from '../stores/editMode'
import { TrashCan20, SettingsEdit20, Star20, StarFilled20 } from '@carbon/icons-vue'
import { onMounted, watch, ref, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { usePagesStore } from '../stores/pages'
import { useFavoritesStore } from '../stores/favorites'
import { useStaticSettingsStore } from '../stores/staticSettings'
import { useToastStore } from '../stores/toast'
import ErrorComponent from '../components/DashboardComponents/ErrorComponent.vue'
import MetadataEditor from '../components/DashboardComponents/MetadataEditor.vue'
import CommentsPanel from '../components/CommentsPanel.vue'
import { pageVersionsService, type PageVersionItem } from '../services/pageVersionsService'
import type { GridItemWithComponent } from '../composables/useGridLayout'
import type { Page } from '../types/page'

const route = useRoute()
const gridLayout = useGridLayout()
const editModeStore = useEditModeStore()
const { getComponent } = useComponentRegistry()
const authStore = useAuthStore()
const pagesStore = usePagesStore()
const favoritesStore = useFavoritesStore()
const staticSettingsStore = useStaticSettingsStore()
const toastStore = useToastStore()

// Modal state
const isConfigModalOpen = ref(false)
const configModalItem = ref<GridItemWithComponent | null>(null)
const configModalValue = ref<Record<string, unknown>>({})
const configModalMetadata = ref<GridItemWithComponent['metadata']>({
  title: '',
  description: '',
  createdAt: '',
  updatedAt: ''
})

const activeTab = ref('general')
const isCommentsOpen = ref(false)
const commentsEnabled = computed(() => staticSettingsStore.commentsEnabled)
const isVersionsOpen = ref(false)
const versionsLoading = ref(false)
const versions = ref<PageVersionItem[]>([])
const rollbackInProgress = ref<number | null>(null)

// Force refresh key to bust cache
const layoutKey = ref(0)
const currentPageId = computed(() => Number(route.params.id))

function findPageById(pages: { id: number; children?: unknown[] }[], id: number): Page | null {
  for (const page of pages) {
    if (page.id === id) return page as Page
    if (page.children && Array.isArray(page.children)) {
      const match = findPageById(page.children as Page[], id)
      if (match) return match
    }
  }
  return null
}

const currentPage = computed(() => findPageById(pagesStore.pages, currentPageId.value))
const isFavorite = computed(() => favoritesStore.isFavorite(currentPageId.value))

// Load layout when component mounts or route changes
onMounted(async () => {
  const pageId = route.params.id as string
  if (pageId) {
    await gridLayout.loadLayout(pageId)
    layoutKey.value++
  }

  if (pagesStore.pages.length === 0) {
    await pagesStore.fetchPages()
  }

  if (authStore.isAuthenticated && !favoritesStore.isLoaded) {
    await favoritesStore.loadAll()
  }

  if (authStore.isAuthenticated && currentPageId.value) {
    await favoritesStore.recordRecent(currentPageId.value)
  }

  if (!staticSettingsStore.isLoaded) {
    await staticSettingsStore.load()
  }
})

// Watch for route changes
watch(() => route.params.id, async (newId) => {
  if (newId) {
    await gridLayout.loadLayout(newId as string)
    layoutKey.value++

    if (authStore.isAuthenticated) {
      await favoritesStore.recordRecent(Number(newId))
    }
  }
})

watch(commentsEnabled, (enabled) => {
  if (!enabled) {
    isCommentsOpen.value = false
  }
})

watch(() => editModeStore.isEditMode, (enabled) => {
  if (!enabled) {
    isVersionsOpen.value = false
  }
})

watch([isVersionsOpen, currentPageId], async ([open]) => {
  if (open && editModeStore.isEditMode) {
    await loadVersions()
  }
})

const parseVersionLayout = (layoutJson: string): GridItemWithComponent[] => {
  try {
    const parsed = JSON.parse(layoutJson)
    return Array.isArray(parsed) ? (parsed as GridItemWithComponent[]) : []
  } catch {
    return []
  }
}

const getDiffSummary = (version: PageVersionItem): string => {
  const current = gridLayout.layout.value
  const historical = parseVersionLayout(version.layout)

  const currentMap = new Map(current.map(item => [item.i, item]))
  const historicalMap = new Map(historical.map(item => [item.i, item]))

  let changed = 0
  for (const [id, item] of historicalMap) {
    const now = currentMap.get(id)
    if (!now) continue
    if (now.x !== item.x || now.y !== item.y || now.w !== item.w || now.h !== item.h || now.componentType !== item.componentType) {
      changed++
    }
  }

  const added = current.filter(item => !historicalMap.has(item.i)).length
  const removed = historical.filter(item => !currentMap.has(item.i)).length
  return `${changed} changed, ${added} added, ${removed} removed`
}

const formatVersionTime = (value: string) => new Date(value).toLocaleString()

const toggleVersions = async () => {
  isVersionsOpen.value = !isVersionsOpen.value
  if (isVersionsOpen.value) {
    await loadVersions()
  }
}

const loadVersions = async () => {
  if (!currentPageId.value || !editModeStore.isEditMode) {
    return
  }

  versionsLoading.value = true
  try {
    versions.value = await pageVersionsService.getByPage(currentPageId.value)
  } catch (error) {
    console.error('Failed to load page versions', error)
    toastStore.error('Error', 'Failed to load page versions')
  } finally {
    versionsLoading.value = false
  }
}

const rollbackVersion = async (version: PageVersionItem) => {
  if (!currentPageId.value) {
    return
  }

  rollbackInProgress.value = version.id
  try {
    await pageVersionsService.rollback(currentPageId.value, version.id, `Rollback via UI to version #${version.id}`)
    await gridLayout.loadLayout(String(currentPageId.value))
    layoutKey.value++
    await loadVersions()
    toastStore.success('Success', 'Layout rolled back successfully')
  } catch (error) {
    console.error('Failed to rollback layout', error)
    toastStore.error('Error', 'Failed to rollback layout')
  } finally {
    rollbackInProgress.value = null
  }
}

const toggleFavorite = async () => {
  if (!authStore.isAuthenticated || !currentPageId.value) {
    return
  }

  await favoritesStore.toggleFavorite(currentPageId.value)
}

const removePanel = (id: string) => {
  gridLayout.removePanel(id)
}

/**
 * Resolve component for a grid item
 * Returns the registered component or ErrorComponent as fallback
 */
const resolveComponent = (item: GridItemWithComponent) => {
  const componentDef = getComponent(item.componentType)

  if (componentDef) {
    return componentDef.component
  }

  // Return error component if not found
  console.warn(`Component type "${item.componentType}" not found in registry`)
  return ErrorComponent
}

/**
 * Open config modal for a panel
 */
const openConfigModal = (item: GridItemWithComponent) => {
  configModalItem.value = item
  configModalValue.value = { ...item.componentConfig }
  configModalMetadata.value = { ...item.metadata }

  isConfigModalOpen.value = true
}

/**
 * Save config changes
 */
const saveConfig = () => {
  if (configModalItem.value) {
    const itemId = configModalItem.value.i

    // Update timestamp
    const updatedMetadata = {
      ...configModalMetadata.value,
      updatedAt: new Date().toISOString()
    }

    // Update both config and metadata
    gridLayout.updatePanelConfig(itemId, configModalValue.value, updatedMetadata)

    // Close modal and reset state
    isConfigModalOpen.value = false
    configModalItem.value = null
    configModalValue.value = {}
    configModalMetadata.value = {
      title: '',
      description: '',
      createdAt: '',
      updatedAt: ''
    }
  }
}

/**
 * Cancel config changes
 */
const cancelConfig = () => {
  isConfigModalOpen.value = false
  configModalItem.value = null
  configModalValue.value = {}
  configModalMetadata.value = {
    title: '',
    description: '',
    createdAt: '',
    updatedAt: ''
  }
}

/**
 * Get config component for a grid item
 */
const getConfigComponent = (item: GridItemWithComponent) => {
  const componentDef = getComponent(item.componentType)
  return componentDef?.configComponent || null
}


</script>

<template>
  <div class="page-view">
    <!-- Loading state -->
    <div v-if="gridLayout.isLoading.value" class="loading-state">
      <cds-loading />
      <p>Loading page layout...</p>
    </div>

    <div v-if="!gridLayout.isLoading.value" class="page-header">
      <div class="page-title-wrap">
        <div class="page-title">{{ currentPage?.title || 'Page' }}</div>
        <span v-if="currentPage?.sensitivityLabel" class="sensitivity-badge" :class="`sensitivity-${(currentPage.sensitivityLabel || '').toLowerCase()}`">
          {{ currentPage.sensitivityLabel }}
        </span>
      </div>
      <div class="page-actions">
        <cds-button v-if="editModeStore.isEditMode" kind="ghost" size="sm" @click="toggleVersions">
          {{ isVersionsOpen ? 'Close Versions' : 'Version History' }}
        </cds-button>
        <cds-button v-if="commentsEnabled" kind="ghost" size="sm" @click="isCommentsOpen = !isCommentsOpen">
          {{ isCommentsOpen ? 'Close Comments' : 'Comments' }}
        </cds-button>
        <cds-button v-if="authStore.isAuthenticated" kind="ghost" size="sm" @click="toggleFavorite">
          <component :is="isFavorite ? StarFilled20 : Star20" slot="icon" />
        </cds-button>
      </div>
    </div>

    <GridLayout v-if="!gridLayout.isLoading.value" v-model:layout="gridLayout.layout.value" :key="`layout-${layoutKey}`"
      :col-num="12" :row-height="30" :is-draggable="editModeStore.isEditMode" :is-resizable="editModeStore.isEditMode"
      :vertical-compact="false" :use-css-transforms="true" :margin="[10, 10]"
      @layout-updated="gridLayout.onLayoutUpdated" class="grid-container">
      <GridItem v-for="item in (gridLayout.layout.value as GridItemWithComponent[])" :key="`${item.i}-${layoutKey}`"
        :x="item.x" :y="item.y" :w="item.w" :h="item.h" :i="item.i" :min-w="item.minW" :min-h="item.minH"
        class="grid-item">
        <div class="panel-content">
          <div class="panel-header" v-if="item.metadata.title && !editModeStore.isEditMode || editModeStore.isEditMode">
            <span class="panel-title" v-if="item.metadata.title">{{ item.metadata.title }}</span>
            <div class="panel-actions" v-if="editModeStore.isEditMode">
              <cds-button kind="ghost" size="sm" @click="openConfigModal(item)" class="config-button"
                :disabled="!getConfigComponent(item)">
                <SettingsEdit20 slot="icon" />
              </cds-button>
              <cds-button kind="ghost" size="sm" @click="removePanel(item.i)" class="remove-button">
                <TrashCan20 slot="icon" />
              </cds-button>
            </div>
          </div>
          <div class="panel-body">
            <!-- Dynamic component with proper props -->
            <component :is="resolveComponent(item)" :id="`component-${item.i}`"
              :key="`component-${item.i}-${layoutKey}`" :config="item.componentConfig"
              :dimensions="{ w: item.w, h: item.h }" :component-type="item.componentType" class="dynamic-component" />
          </div>
        </div>
      </GridItem>
    </GridLayout>

    <div v-if="!gridLayout.isLoading.value && gridLayout.layout.value.length === 0" class="empty-state">
      <p>No panels configured for this page. Use the Tools menu to add panels.</p>
    </div>

    <aside v-if="editModeStore.isEditMode && isVersionsOpen" class="versions-panel">
      <div class="versions-header">
        <h3>Version History</h3>
        <div class="versions-header-actions">
          <cds-button kind="ghost" size="sm" @click="loadVersions">Refresh</cds-button>
          <cds-button kind="ghost" size="sm" @click="isVersionsOpen = false">Close</cds-button>
        </div>
      </div>

      <p v-if="versionsLoading" class="versions-loading">Loading versions...</p>
      <p v-else-if="versions.length === 0" class="versions-empty">No versions saved yet.</p>

      <div v-else class="versions-list">
        <article v-for="version in versions" :key="version.id" class="version-card">
          <div class="version-meta">
            <strong>#{{ version.id }}</strong>
            <span>{{ formatVersionTime(version.changedAt) }}</span>
          </div>
          <div class="version-by">By {{ version.changedBy }}</div>
          <div class="version-desc">{{ version.changeDescription || 'Layout saved' }}</div>
          <div class="version-diff">Diff: {{ getDiffSummary(version) }}</div>
          <cds-button kind="secondary" size="sm" :disabled="rollbackInProgress === version.id" @click="rollbackVersion(version)">
            {{ rollbackInProgress === version.id ? 'Rolling back...' : 'Rollback to this version' }}
          </cds-button>
        </article>
      </div>
    </aside>

    <CommentsPanel v-if="commentsEnabled" :open="isCommentsOpen" :page-id="currentPageId" @close="isCommentsOpen = false" />

    <!-- Config Modal -->
    <cds-modal :open="isConfigModalOpen" @cds-modal-closed="cancelConfig" size="lg" class="config-modal">
      <cds-modal-header>
        <cds-modal-close-button></cds-modal-close-button>
        <cds-modal-heading>Settings</cds-modal-heading>
      </cds-modal-header>
      <cds-modal-body>
        <div class="config-modal-wrapper">
          <cds-tabs :value="activeTab" @cds-tabs-selected="(e: CustomEvent) => activeTab = e.detail.item.value"
            class="config-tabs">
            <cds-tab value="general">General</cds-tab>
            <cds-tab v-if="configModalItem && getConfigComponent(configModalItem)" value="component">Component</cds-tab>
          </cds-tabs>
          <div class="cds-tab-content">
            <div v-show="activeTab === 'general'" role="tabpanel">
              <MetadataEditor v-model="configModalMetadata" />
            </div>
            <div v-if="configModalItem && getConfigComponent(configModalItem)" v-show="activeTab === 'component'"
              role="tabpanel">
              <component 
                :is="getConfigComponent(configModalItem)" 
                v-model="configModalValue"
                :component-type="configModalItem.componentType" />
            </div>
          </div>
        </div>
      </cds-modal-body>
      <cds-modal-footer>
        <cds-modal-footer-button kind="secondary" @click="cancelConfig">Cancel</cds-modal-footer-button>
        <cds-modal-footer-button kind="primary" @click="saveConfig">Save</cds-modal-footer-button>
      </cds-modal-footer>
    </cds-modal>
  </div>
</template>


<style scoped>
.page-view {
  display: flex;
  flex-direction: column;
  height: 100%;
  /* padding: 1rem; */
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.75rem 0.5rem;
}

.page-title-wrap {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.page-title {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--cds-text-primary);
}

.sensitivity-badge {
  display: inline-flex;
  align-items: center;
  border-radius: 999px;
  padding: 0.1rem 0.5rem;
  font-size: 0.75rem;
  font-weight: 600;
  border: 1px solid transparent;
}

.sensitivity-public {
  background: #e8f5e9;
  color: #0e6027;
  border-color: #8fd19e;
}

.sensitivity-internal {
  background: #edf5ff;
  color: #0f62fe;
  border-color: #a6c8ff;
}

.sensitivity-confidential {
  background: #fff4e5;
  color: #8a3800;
  border-color: #f1c21b;
}

.sensitivity-restricted {
  background: #fff1f1;
  color: #a2191f;
  border-color: #ff8389;
}

@media (prefers-color-scheme: dark) {
  .page-description {
    color: #c6c6c6;
  }
}

.grid-item {
  background: white;
  border-radius: 4px;
  border: 1px solid #e0e0e0;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  transition: box-shadow 0.2s ease;
}

.grid-item:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
}

@media (prefers-color-scheme: dark) {
  .grid-item {
    /* background: #ebdfdf; */
    border-color: #393939;
  }
}

.panel-content {
  height: 100%;
  display: flex;
  flex-direction: column;
  padding: 0.5rem;
}

.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.75rem;
  padding-bottom: 0.5rem;
  border-bottom: 1px solid #e0e0e0;
}

@media (prefers-color-scheme: dark) {
  .panel-header {
    border-bottom-color: #393939;
  }
}

.panel-actions {
  display: flex;
  gap: 0.25rem;
  align-items: center;
  margin-left: auto;
}



.panel-title {
  font-weight: 600;
  font-size: 0.875rem;
  color: #161616;
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.config-button,
.remove-button {
  padding: 0.25rem;
  min-height: auto;
}

.panel-body {
  flex: 1;
  overflow: auto;
  padding: 0.25rem 0;
}

.dynamic-component {
  width: 100%;
  height: 100%;
}

.panel-body p {
  margin: 0 0 0.5rem 0;
  font-size: 0.875rem;
}

.panel-info {
  color: #525252;
  font-size: 0.75rem;
}

@media (prefers-color-scheme: dark) {
  .panel-info {
    color: #c6c6c6;
  }
}

.empty-state {
  text-align: center;
  padding: 4rem 2rem;
  color: #525252;
  font-size: 1rem;
}

.versions-panel {
  position: fixed;
  top: 3.5rem;
  right: 0;
  width: min(430px, 95vw);
  height: calc(100vh - 3.5rem);
  background: var(--cds-layer);
  border-left: 1px solid var(--cds-border-subtle);
  z-index: 1200;
  padding: 1rem;
  overflow: auto;
}

.versions-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.75rem;
}

.versions-header-actions {
  display: flex;
  gap: 0.35rem;
}

.versions-header h3 {
  margin: 0;
}

.versions-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.version-card {
  border: 1px solid var(--cds-border-subtle);
  background: var(--cds-layer-accent);
  padding: 0.75rem;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.version-meta {
  display: flex;
  justify-content: space-between;
  font-size: 0.85rem;
  color: var(--cds-text-secondary);
}

.version-by,
.version-desc,
.version-diff,
.versions-loading,
.versions-empty {
  font-size: 0.85rem;
  color: var(--cds-text-secondary);
}

@media (prefers-color-scheme: dark) {
  .empty-state {
    color: #c6c6c6;
  }
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  min-height: 400px;
  gap: 1rem;
}

.loading-state p {
  color: #525252;
  font-size: 0.875rem;
  margin: 0;
}

@media (prefers-color-scheme: dark) {
  .loading-state p {
    color: #c6c6c6;
  }
}

.config-modal :deep(.bx--modal-container) {
  min-height: 500px;
}

.config-modal :deep(.bx--modal-content) {
  min-height: 400px;
  padding-bottom: 0;
}

.config-modal-wrapper {
  height: 400px;
  overflow-y: auto;
}

.config-tabs {
  margin: 0;
  height: 100%;
}

.no-config {
  padding: 2rem;
  text-align: center;
  color: #525252;
}
</style>