<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { authService } from '../services/auth.service'
import '@carbon/web-components/es/components/inline-loading/index.js'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()

const error = ref('')

function normalizeNextPath(value: unknown): string {
    if (typeof value !== 'string' || !value.startsWith('/') || value.startsWith('//')) {
        return '/'
    }

    return value
}

onMounted(() => {
    const result = authService.parseExternalCallbackHash(window.location.hash)
    if (result.token) {
        auth.setToken(result.token)
        const nextPath = normalizeNextPath(route.query.next)
        router.replace(nextPath)
        return
    }

    error.value = result.error || 'External login failed'
    router.replace({
        path: '/login',
        query: {
            error: error.value
        }
    })
})
</script>

<template>
  <div class="callback-container">
    <cds-inline-loading status="active">Completing sign in</cds-inline-loading>
  </div>
</template>

<style scoped>
.callback-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>
