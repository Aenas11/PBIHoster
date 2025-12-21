<!-- component that renders simple HTML content from config  -->
<script setup lang="ts">
import { computed } from 'vue'
import type { DashboardComponentProps } from '../../types/components'

const props = defineProps<DashboardComponentProps>()

// Use computed to ensure reactivity when config changes
const htmlContent = computed(() => {
  return props.config.content || '<p>No content configured</p>'
})
</script>

<template>
  <div v-html="htmlContent" class="simple-html-content"></div>
</template>

<style scoped>
.simple-html-content {
  width: 100%;
  height: 100%;
  overflow: auto;  
}
</style>