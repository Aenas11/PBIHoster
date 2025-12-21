<!-- Fallback component when a component type is not found -->
<script setup lang="ts">
import type { DashboardComponentProps } from '../../types/components'

defineProps<DashboardComponentProps & { 
  componentType?: string
  error?: string 
}>()
</script>

<template>
  <div class="error-component">
    <div class="error-icon">⚠️</div>
    <h3>Component Error</h3>
    <p v-if="componentType" class="error-message">
      Component type "{{ componentType }}" not found
    </p>
    <p v-else-if="error" class="error-message">
      {{ error }}
    </p>
    <p v-else class="error-message">
      Unknown component error
    </p>
    <p class="error-hint">
      Please check the component registry or remove this panel.
    </p>
  </div>
</template>

<style scoped>
.error-component {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  text-align: center;
  padding: 1rem;
  color: #da1e28;
}

@media (prefers-color-scheme: dark) {
  .error-component {
    color: #fa4d56;
  }
}

.error-icon {
  font-size: 2rem;
  margin-bottom: 0.5rem;
}

.error-component h3 {
  margin: 0 0 0.5rem 0;
  font-size: 1rem;
  font-weight: 600;
}

.error-message {
  margin: 0 0 0.5rem 0;
  font-size: 0.875rem;
  font-weight: 500;
}

.error-hint {
  margin: 0;
  font-size: 0.75rem;
  color: #525252;
}

@media (prefers-color-scheme: dark) {
  .error-hint {
    color: #c6c6c6;
  }
}
</style>
