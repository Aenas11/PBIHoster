<script setup lang="ts">
defineProps<{
  expanded: boolean
}>()
</script>

<template>
  <cv-header-panel id="tools-panel" :expanded="expanded">
    <div class="tools-panel-content">
      <h3 class="tools-title">Tools Menu</h3>
      <p class="tools-description">Tools content goes here.</p>
      <!-- Future: Add page editing tools, settings, etc. -->
    </div>
  </cv-header-panel>
</template>

<style scoped>
.tools-panel-content {
  padding: 1rem;
  width: 200px;
}

.tools-title {
  margin: 0 0 1rem 0;
  font-size: 1rem;
  font-weight: 600;
}

.tools-description {
  margin: 0;
  font-size: 0.875rem;
  color: #525252;
}

@media (prefers-color-scheme: dark) {
  .tools-description {
    color: #c6c6c6;
  }
}
</style>
