<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useStaticSettingsStore } from '../stores/staticSettings'
import { authService, type ExternalAuthProviderSummary } from '../services/auth.service'
import '@carbon/web-components/es/components/tile/index.js';
import '@carbon/web-components/es/components/text-input/index.js';
import '@carbon/web-components/es/components/button/index.js';
import '@carbon/web-components/es/components/link/index.js';

const router = useRouter()
const currentPath = window.location.pathname + window.location.search
const auth = useAuthStore()
const staticSettings = useStaticSettingsStore()
const appName = computed(() => staticSettings.appName || 'ReportTree')
const logoUrl = computed(() => staticSettings.logoUrl || '')

const username = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)
const externalProviders = ref<ExternalAuthProviderSummary[]>([])
const loadingProviders = ref(false)
const externalLoginLoadingId = ref('')

onMounted(async () => {
  const params = new URLSearchParams(window.location.search)
  const callbackError = params.get('error')
  if (callbackError) {
    error.value = callbackError
  }

  loadingProviders.value = true
  try {
    externalProviders.value = await authService.getExternalProviders()
  } catch {
    externalProviders.value = []
  } finally {
    loadingProviders.value = false
  }
})

async function handleLogin() {
  loading.value = true
  error.value = ''

  try {
    await auth.login(username.value, password.value)
    router.push('/')
  } catch {
    error.value = 'Invalid username or password'
  } finally {
    loading.value = false
  }
}

function handleExternalLogin(provider: ExternalAuthProviderSummary) {
  externalLoginLoadingId.value = provider.id
  const callbackReturn = `/auth/callback?next=${encodeURIComponent(currentPath === '/login' ? '/' : currentPath)}`
  authService.startExternalLogin(provider.id, callbackReturn)
}
</script>

<template>
  <div class="login-container">
    <div class="login-card">
      <cds-tile>
        <div class="login-brand">
          <img v-if="logoUrl" :src="logoUrl" class="login-logo" alt="" aria-hidden="true" />
          <h2>Login to {{ appName }}</h2>
        </div>

        <cds-text-input :value="username" @input="username = ($event.target as HTMLInputElement).value" label="Username"
          placeholder="Enter username" style="margin-bottom: 1rem;"></cds-text-input>

        <cds-text-input :value="password" @input="password = ($event.target as HTMLInputElement).value" label="Password"
          type="password" placeholder="Enter password" style="margin-bottom: 1rem;"
          @keyup.enter="handleLogin"></cds-text-input>

        <div v-if="error" style="color: #da1e28; margin-bottom: 1rem;">
          {{ error }}
        </div>

        <div style="display: flex; justify-content: space-between; align-items: center;">
          <cds-link href="/register">Register</cds-link>
          <cds-button @click="handleLogin" :disabled="loading">
            {{ loading ? 'Logging in...' : 'Login' }}
          </cds-button>
        </div>

        <div v-if="loadingProviders" class="external-auth-loading">Checking external sign-in options...</div>

        <div v-if="externalProviders.length > 0" class="external-auth">
          <p class="external-auth-title">Or continue with</p>
          <div class="external-auth-buttons">
            <cds-button
              v-for="provider in externalProviders"
              :key="provider.id"
              kind="secondary"
              :disabled="!!externalLoginLoadingId"
              @click="handleExternalLogin(provider)">
              {{ externalLoginLoadingId === provider.id ? `Redirecting to ${provider.displayName}...` : `Sign in with ${provider.displayName}` }}
            </cds-button>
          </div>
        </div>
      </cds-tile>
    </div>
  </div>
</template>

<style scoped>
.login-container {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100vh;
  background-color: #f4f4f4;
}

.login-card {
  width: 100%;
  max-width: 400px;
  padding: 1rem;
}

.login-brand {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 1rem;
}

.login-logo {
  height: 32px;
  width: auto;
}

.external-auth-loading {
  margin-top: 1rem;
  color: var(--cds-text-secondary, #525252);
  font-size: 0.875rem;
}

.external-auth {
  margin-top: 1.25rem;
  border-top: 1px solid var(--cds-border-subtle-01, #e0e0e0);
  padding-top: 1rem;
}

.external-auth-title {
  margin: 0 0 0.75rem 0;
  color: var(--cds-text-secondary, #525252);
  font-size: 0.875rem;
}

.external-auth-buttons {
  display: grid;
  gap: 0.5rem;
}
</style>
