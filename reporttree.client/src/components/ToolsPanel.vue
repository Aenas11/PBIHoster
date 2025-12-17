<script setup lang="ts">
import { Add20, TrashCan20 } from '@carbon/icons-vue'
import { useGridLayout } from '../composables/useGridLayout'

defineProps<{
  expanded: boolean
}>()

const gridLayout = useGridLayout()

const handleAddPanel = () => {
  gridLayout.addPanel()
}

const handleClearLayout = () => {
  gridLayout.clearLayout()
}
</script>

<template>
  <cv-header-panel id="tools-panel" :expanded="expanded">
    <div class="tools-panel-content">
      <h3 class="tools-title">Tools Menu</h3>
      
      <div class="tools-section">
        <h4 class="section-title">Layout Tools</h4>
        
        <cv-button 
          kind="primary" 
          size="sm" 
          class="tool-button"
          @click="handleAddPanel"
        >
          <Add20 class="button-icon" />
          Add Panel
        </cv-button>
        
        <cv-button 
          kind="danger" 
          size="sm" 
          class="tool-button"
          @click="handleClearLayout"
        >
          <TrashCan20 class="button-icon" />
          Clear Layout
        </cv-button>
      </div>

      <div class="tools-section">
        <h4 class="section-title">Panel Count</h4>
        <p class="panel-count">{{ gridLayout.layout.value.length }} panels</p>
      </div>
    </div>
  </cv-header-panel>
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
</style>
