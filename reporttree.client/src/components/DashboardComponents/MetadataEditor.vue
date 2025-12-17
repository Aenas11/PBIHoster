<!-- Shared metadata editor for all grid items -->
<script setup lang="ts">
import { ref, watch } from 'vue'
import type { GridItemWithComponent } from '../../types/layout'

interface Props {
  modelValue: GridItemWithComponent['metadata']
}

interface Emits {
  (e: 'update:modelValue', value: GridItemWithComponent['metadata']): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

// Local state for form fields
const title = ref(props.modelValue.title)
const description = ref(props.modelValue.description)

// Watch for external changes
watch(() => props.modelValue, (newValue) => {
  title.value = newValue.title
  description.value = newValue.description
}, { deep: true })

// Emit changes
const updateTitle = (value: string) => {
  emit('update:modelValue', {
    ...props.modelValue,
    title: value
  })
}

const updateDescription = (value: string) => {
  emit('update:modelValue', {
    ...props.modelValue,
    description: value
  })
}

// Format date for display
const formatDate = (dateStr: string) => {
  if (!dateStr) return 'N/A'
  try {
    const date = new Date(dateStr)
    return date.toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  } catch {
    return dateStr
  }
}
</script>

<template>
  <div class="metadata-editor">
    <div class="metadata-section">

      <cv-text-input v-model="title" label="Title" placeholder="Enter panel title" @update:modelValue="updateTitle" />

      <cv-text-area v-model="description" label="Description" placeholder="Enter panel description"
        @update:modelValue="updateDescription" :rows="3" />
    </div>

    <div class="timestamps">
      <div class="timestamp-item">
        <span class="timestamp-label">Created</span>
        <span class="timestamp-value">{{ formatDate(modelValue.createdAt) }}</span>
      </div>

      <div class="timestamp-item">
        <span class="timestamp-label">Updated</span>
        <span class="timestamp-value">{{ formatDate(modelValue.updatedAt) }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.metadata-editor {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  position: relative;
  min-height: 200px;
}

.metadata-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.section-title {
  font-size: 0.875rem;
  font-weight: 600;
  color: #161616;
  margin: 0 0 0.5rem 0;
  padding-bottom: 0.5rem;
  /* border-bottom: 1px solid #e0e0e0; */
}

.timestamps {
  display: flex;
  gap: 1.5rem;
  padding-top: 0.5rem;
  margin-top: auto;
  border-top: 1px solid #e0e0e0;
  align-self: flex-end;
}

@media (prefers-color-scheme: dark) {
  .timestamps {
    border-top-color: #393939;
  }
}

.timestamp-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.timestamp-label {
  font-size: 0.75rem;
  font-weight: 400;
  color: #525252;
  text-transform: uppercase;
  letter-spacing: 0.32px;
}

.timestamp-value {
  font-size: 0.875rem;
  font-weight: 400;
  color: #161616;
  line-height: 1.29;
}

@media (prefers-color-scheme: dark) {
  .timestamp-label {
    color: #8d8d8d;
  }

  .timestamp-value {
    /* color: #f4f4f4; */
  }
}
</style>
