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
import { TrashCan20, SettingsEdit20 } from '@carbon/icons-vue'
import { onMounted, watch, ref } from 'vue'
import { useRoute } from 'vue-router'
import ErrorComponent from '../components/DashboardComponents/ErrorComponent.vue'
import MetadataEditor from '../components/DashboardComponents/MetadataEditor.vue'
import type { GridItemWithComponent } from '../composables/useGridLayout'

const route = useRoute()
const gridLayout = useGridLayout()
const editModeStore = useEditModeStore()
const { getComponent } = useComponentRegistry()

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

// Load layout when component mounts or route changes
onMounted(async () => {
  const pageId = route.params.id as string
  if (pageId) {
    await gridLayout.loadLayout(pageId)
  }
})

// Watch for route changes
watch(() => route.params.id, async (newId) => {
  if (newId) {
    await gridLayout.loadLayout(newId as string)
  }
})

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

    <GridLayout v-else v-model:layout="gridLayout.layout.value" :col-num="12" :row-height="30"
      :is-draggable="editModeStore.isEditMode" :is-resizable="editModeStore.isEditMode" :vertical-compact="false"
      :use-css-transforms="true" :margin="[10, 10]" @layout-updated="gridLayout.onLayoutUpdated" class="grid-container">
      <GridItem v-for="item in (gridLayout.layout.value as GridItemWithComponent[])" :key="item.i" :x="item.x"
        :y="item.y" :w="item.w" :h="item.h" :i="item.i" :min-w="item.minW" :min-h="item.minH" class="grid-item">
        <div class="panel-content">
          <div class="panel-header">
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
            <component :is="resolveComponent(item)" :id="`component-${item.i}`" :key="`component-${item.i}`"
              :config="item.componentConfig" :dimensions="{ w: item.w, h: item.h }" :component-type="item.componentType"
              class="dynamic-component" />
          </div>
        </div>
      </GridItem>
    </GridLayout>

    <div v-if="!gridLayout.isLoading.value && gridLayout.layout.value.length === 0" class="empty-state">
      <p>No panels configured for this page. Use the Tools menu to add panels.</p>
    </div>

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
              <component :is="getConfigComponent(configModalItem)" v-model="configModalValue" />
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
  padding: 1rem;
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