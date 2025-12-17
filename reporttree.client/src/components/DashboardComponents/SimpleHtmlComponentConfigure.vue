<!-- Configuration component for SimpleHtmlComponent  -->
<script setup lang="ts">
import '@carbon/web-components/es/components/textarea/index.js';
import type { ComponentConfigProps } from '../../types/components'
import { ref, watch } from 'vue'

const props = defineProps<ComponentConfigProps>()
const emit = defineEmits<{
  'update:modelValue': [value: Record<string, unknown>]
}>()// Local state for the textarea
const content = ref(props.modelValue.content || '')

// Emit updates when content changes
watch(content, (newContent) => {
  emit('update:modelValue', {
    ...props.modelValue,
    content: newContent
  })
})
</script>

<template>
  <div class="config-container">
    <cds-textarea :value="content" label="HTML Content" placeholder="Enter HTML content..." rows="10"
      helper-text="Enter the HTML content to display in the component"
      @input="content = ($event.target as HTMLTextAreaElement).value" />

    <div class="preview-section">
      <h4>Preview</h4>
      <div v-html="content" class="preview-content"></div>
    </div>
  </div>
</template>

<style scoped>
.preview-section {
  border: 1px solid #e0e0e0;
  padding: 1rem;
  border-radius: 4px;
}

@media (prefers-color-scheme: dark) {
  .preview-section {
    border-color: #393939;
  }
}

.preview-section h4 {
  margin: 0 0 0.5rem 0;
  font-size: 0.875rem;
  font-weight: 600;
}

.preview-content {
  min-height: 100px;
  background: rgba(0, 0, 0, 0.02);
  padding: 0.5rem;
  border-radius: 4px;
}

@media (prefers-color-scheme: dark) {
  .preview-content {
    background: rgba(255, 255, 255, 0.05);
  }
}
</style>