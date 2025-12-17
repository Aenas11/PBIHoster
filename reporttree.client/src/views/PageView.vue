<!-- page with dynamic layout -->
<script setup lang="ts">
import { GridLayout, GridItem } from 'grid-layout-plus'
import type { Layout } from 'grid-layout-plus'
import { useGridLayout } from '../composables/useGridLayout'
import { TrashCan20 } from '@carbon/icons-vue'

const gridLayout = useGridLayout()

const handleLayoutUpdated = (newLayout: Layout) => {
  gridLayout.updateLayout(newLayout)
}

const removePanel = (id: string) => {
  gridLayout.removePanel(id)
}
</script>

<template>
  <div class="page-view">
    <div class="page-header">
      <h1>Page View</h1>
      <p class="page-description">Dynamic grid layout with draggable and resizable panels</p>
    </div>
    
    <GridLayout
      v-model:layout="gridLayout.layout.value"
      :col-num="12"
      :row-height="30"
      :is-draggable="true"
      :is-resizable="true"
      :vertical-compact="true"
      :use-css-transforms="true"
      :margin="[10, 10]"
      @layout-updated="handleLayoutUpdated"
      class="grid-container"
    >
      <GridItem
        v-for="item in gridLayout.layout.value"
        :key="item.i"
        :x="item.x"
        :y="item.y"
        :w="item.w"
        :h="item.h"
        :i="item.i"
        :min-w="item.minW"
        :min-h="item.minH"
        class="grid-item"
      >
        <div class="panel-content">
          <div class="panel-header">
            <span class="panel-id">{{ item.i }}</span>
            <cv-button
              kind="ghost"
              size="sm"
              @click="removePanel(item.i)"
              class="remove-button"
            >
              <TrashCan20 />
            </cv-button>
          </div>
          <div class="panel-body">
            <p>Panel content goes here</p>
            <p class="panel-info">Position: ({{ item.x }}, {{ item.y }})</p>
            <p class="panel-info">Size: {{ item.w }} × {{ item.h }}</p>
          </div>
        </div>
      </GridItem>
    </GridLayout>

    <div v-if="gridLayout.layout.value.length === 0" class="empty-state">
      <p>No panels yet. Use the Tools menu to add panels.</p>
    </div>
  </div>
</template>

<style scoped>
.page-view {
  padding: 2rem;
  min-height: 100%;
}

.page-header {
  margin-bottom: 2rem;
}

.page-header h1 {
  margin: 0 0 0.5rem 0;
  font-size: 2rem;
  font-weight: 600;
}

.page-description {
  margin: 0;
  color: #525252;
  font-size: 1rem;
}

@media (prefers-color-scheme: dark) {
  .page-description {
    color: #c6c6c6;
  }
}

.grid-container {
  min-height: 500px;
  background: rgba(0, 0, 0, 0.02);
  border-radius: 8px;
  padding: 10px;
}

@media (prefers-color-scheme: dark) {
  .grid-container {
    background: rgba(255, 255, 255, 0.05);
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
    background: #262626;
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

.panel-id {
  font-weight: 600;
  font-size: 0.875rem;
  color: #161616;
}

@media (prefers-color-scheme: dark) {
  .panel-id {
    color: #f4f4f4;
  }
}

.remove-button {
  padding: 0.25rem;
  min-height: auto;
}

.panel-body {
  flex: 1;
  overflow: auto;
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
</style>