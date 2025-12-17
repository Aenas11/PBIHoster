<!-- component that renders simple HTML content from model  -->
<script setup lang="ts">
const props = defineProps({
  model: {
    type: Object,
    default: () => {}
  }
})
</script>

<template>
  <div v-html="props.model.content"></div>
</template>