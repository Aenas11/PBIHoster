<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import '@carbon/web-components/es/components/tile/index.js';
import '@carbon/web-components/es/components/text-input/index.js';
import '@carbon/web-components/es/components/button/index.js';
import '@carbon/web-components/es/components/link/index.js';

const router = useRouter()
const auth = useAuthStore()

const username = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

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
</script>

<template>
  <div class="login-container">
    <div class="login-card">
      <cds-tile>
        <h2 style="margin-bottom: 1rem;">Login to ReportTree</h2>

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
</style>
