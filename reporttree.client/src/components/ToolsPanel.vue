<script setup lang="ts">
import '@carbon/web-components/es/components/ui-shell/index.js';
import '@carbon/web-components/es/components/button/index.js';
import { ref } from 'vue'
import { Add20, TrashCan20, Save20 } from '@carbon/icons-vue'
import { useGridLayout } from '../composables/useGridLayout'
import { useComponentRegistry } from '../composables/useComponentRegistry'
import { useEditModeStore } from '../stores/editMode'
import { layoutService } from '../services/layoutService'
import { useRoute } from 'vue-router'

defineProps<{
  expanded: boolean
}>()

const route = useRoute()
const gridLayout = useGridLayout()
const editModeStore = useEditModeStore()
const { getAllComponents } = useComponentRegistry()
const isSaving = ref(false)
const saveStatus = ref<{ type: 'success' | 'error', message: string } | null>(null)

// Get available component types
const availableComponents = getAllComponents()

const handleAddPanel = (componentType: string) => {
  if (!editModeStore.isEditMode) {
    console.warn('Cannot add panel: Edit mode is not active')
    return
  }
  gridLayout.addPanel(componentType)
}

const handleClearLayout = () => {
  if (!editModeStore.isEditMode) {
    console.warn('Cannot clear layout: Edit mode is not active')
    return
  }
  gridLayout.clearLayout()
}

const handleSaveLayout = async (pageId: string) => {
  isSaving.value = true
  saveStatus.value = null

  try {

    const response = await layoutService.saveLayout({
      pageId,
      layout: gridLayout.layout.value,
      metadata: {
        name: `Layout ${new Date().toLocaleString()}`,
        createdAt: new Date().toISOString()
      }
    })

    if (response.success) {
      saveStatus.value = { type: 'success', message: 'Layout saved successfully!' }
      // Clear success message after 3 seconds
      setTimeout(() => {
        saveStatus.value = null
      }, 3000)
    } else {
      saveStatus.value = { type: 'error', message: 'Failed to save layout' }
    }
  } catch (error) {
    console.error('Error saving layout:', error)
    saveStatus.value = { type: 'error', message: 'Error saving layout' }
  } finally {
    isSaving.value = false
  }
}
</script>

<template>
  <cds-header-panel id="tools-panel" :expanded="expanded">
    <div class="tools-panel-content">
      <h3 class="tools-title">Tools Menu</h3>

      <div v-if="!editModeStore.isEditMode" class="edit-mode-notice">
        <p>Enable Edit Mode in the side menu to use these tools.</p>
      </div>

      <div class="tools-section">
        <h4 class="section-title">Add Components</h4>

        <cds-button v-for="component in availableComponents" :key="component.type" kind="primary" size="sm"
          class="tool-button" @click="() => handleAddPanel(component.type)" :title="component.description"
          :disabled="!editModeStore.isEditMode">
          <Add20 slot="icon" class="button-icon" />
          {{ component.name }}
        </cds-button>

        <p v-if="availableComponents.length === 0" class="no-components">
          No components registered
        </p>
      </div>

      <div class="tools-section">
        <h4 class="section-title">Layout Actions</h4>


        <cds-button kind="tertiary" size="sm" class="tool-button"
          @click="() => handleSaveLayout(route.params.id as string)"
          :disabled="isSaving || gridLayout.layout.value.length === 0 || !editModeStore.isEditMode">
          <Save20 slot="icon" class="button-icon" />

          {{ isSaving ? 'Saving...' : 'Save Layout' }}
        </cds-button>

        <cds-button kind="danger" size="sm" class="tool-button" @click="handleClearLayout"
          :disabled="!editModeStore.isEditMode">
          <TrashCan20 slot="icon" class="button-icon" />
          Clear Layout
        </cds-button>
      </div>

      <div v-if="saveStatus" class="tools-section">
        <div class="save-status" :class="`save-status--${saveStatus.type}`">
          {{ saveStatus.message }}
        </div>
      </div>

      <div class="tools-section">
        <h4 class="section-title">Panel Count</h4>
        <p class="panel-count">{{ gridLayout.layout.value.length }} panels</p>
      </div>
    </div>
  </cds-header-panel>
</template>


<style scoped>
.tools-panel-content {
  padding: 1rem;
  width: 250px;
}

.tools-title {
  margin: 0 0 1.5rem 0;
  font-size: 1.125rem;
  font-weight: 600;
}

.edit-mode-notice {
  background: rgba(255, 193, 7, 0.1);
  border: 1px solid rgba(255, 193, 7, 0.5);
  border-radius: 4px;
  padding: 0.75rem;
  margin-bottom: 1rem;
}

.edit-mode-notice p {
  margin: 0;
  font-size: 0.875rem;
  color: #525252;
  text-align: center;
}

@media (prefers-color-scheme: dark) {
  .edit-mode-notice {
    background: rgba(255, 193, 7, 0.2);
  }

  .edit-mode-notice p {
    color: #f1c21b;
  }
}

.tools-section {
  margin-bottom: 1.5rem;
}

.section-title {
  margin: 0 0 0.75rem 0;
  font-size: 0.875rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.02em;
  color: #525252;
}

@media (prefers-color-scheme: dark) {
  .section-title {
    color: #c6c6c6;
  }
}

.tool-button {
  width: 100%;
  margin-bottom: 0.5rem;
  justify-content: flex-start;
}

.no-components {
  margin: 0;
  font-size: 0.75rem;
  color: #525252;
  font-style: italic;
  text-align: center;
  padding: 0.5rem;
}

@media (prefers-color-scheme: dark) {
  .no-components {
    color: #c6c6c6;
  }
}

.button-icon {
  margin-right: 0.5rem;
}

.panel-count {
  margin: 0;
  font-size: 0.875rem;
  color: #525252;
  padding: 0.5rem;
  background: rgba(0, 0, 0, 0.05);
  border-radius: 4px;
  text-align: center;
}

@media (prefers-color-scheme: dark) {
  .panel-count {
    color: #c6c6c6;
    background: rgba(255, 255, 255, 0.1);
  }
}

.save-status {
  padding: 0.5rem;
  border-radius: 4px;
  font-size: 0.875rem;
  text-align: center;
  animation: fadeIn 0.3s ease-in;
}

.save-status--success {
  background: #d0e2d0;
  color: #0e6027;
  border: 1px solid #0e6027;
}

.save-status--error {
  background: #ffd7d9;
  color: #750e13;
  border: 1px solid #750e13;
}

@media (prefers-color-scheme: dark) {
  .save-status--success {
    background: #0e6027;
    color: #d0e2d0;
  }

  .save-status--error {
    background: #750e13;
    color: #ffd7d9;
  }
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(-10px);
  }

  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
