<script setup lang="ts">
import { computed, onErrorCaptured, ref } from 'vue'
import { reportClientError } from '../services/monitoring'

const error = ref<Error | null>(null)
const errorInfo = ref<string | null>(null)

const friendlyMessage = computed(() => {
  if (!error.value) return ''
  return error.value.message || 'An unexpected error occurred.'
})

function resetBoundary() {
  error.value = null
  errorInfo.value = null
}

onErrorCaptured(async (err, _instance, info) => {
  error.value = err as Error
  errorInfo.value = info ?? null

  await reportClientError({
    message: err instanceof Error ? err.message : 'Unknown render error',
    stack: err instanceof Error ? err.stack : undefined,
    info
  })

  // do not stop error propagation to allow devtools to see it
  return false
})
</script>

<template>
  <div>
    <div v-if="!error">
      <slot />
    </div>
    <div v-else class="error-boundary">
      <div class="error-boundary__card">
        <h2>Something went wrong</h2>
        <p>{{ friendlyMessage }}</p>
        <p v-if="errorInfo" class="error-boundary__detail">
          Context: {{ errorInfo }}
        </p>
        <button class="error-boundary__button" type="button" @click="resetBoundary">
          Try again
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.error-boundary {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem;
}

.error-boundary__card {
  background: #f4f4f4;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  max-width: 520px;
  width: 100%;
  padding: 1.5rem;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
}

.error-boundary__detail {
  color: #6f6f6f;
}

.error-boundary__button {
  margin-top: 1rem;
  background-color: #0f62fe;
  color: #fff;
  border: none;
  border-radius: 4px;
  padding: 0.75rem 1rem;
  cursor: pointer;
}

.error-boundary__button:hover {
  background-color: #0043ce;
}
</style>
