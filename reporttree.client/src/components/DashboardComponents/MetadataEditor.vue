<!-- Shared metadata editor for all grid items -->
<script setup lang="ts">
import '@carbon/web-components/es/components/text-input/index.js';
import '@carbon/web-components/es/components/textarea/index.js';
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

      <cds-text-input :value="title" label="Title" placeholder="Enter panel title"
        @input="updateTitle(($event.target as HTMLInputElement).value)" />

      <cds-textarea :value="description" label="Description" placeholder="Enter panel description"
        @input="updateDescription(($event.target as HTMLTextAreaElement).value)" rows="3" />
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

<style scoped></style>
