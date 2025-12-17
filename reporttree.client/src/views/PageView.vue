<!-- page with dynamic layout -->
<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const layout = computed(() => {
  return route.meta.layout || 'default'
})
</script>

<template :is="`${layout}-layout`">
  <div>
    <h1>Page View</h1>
    <p>This is a page with dynamic layout: {{ layout }}</p>
  </div>
</template>


<style scoped>
</style>