<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const auth = useAuthStore()

const username = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

async function handleLogin() {
  loading.value = true
  error.value = ''
  
  const success = await auth.login(username.value, password.value)
  
  if (success) {
    router.push('/')
  } else {
    error.value = 'Invalid username or password'
  }
  loading.value = false
}
</script>

<template>
  <div class="login-container">
    <div class="login-card">
      <cv-tile>
        <h2 style="margin-bottom: 1rem;">Login to ReportTree</h2>
        
        <cv-text-input
          v-model="username"
          aria-required="true"
          label="Username"
          placeholder="Enter username"
          style="margin-bottom: 1rem;"
        />
        
        <cv-text-input
              v-model="password"
              aria-required="true"
              label="Password"
              type="password"
              placeholder="Enter password"
              style="margin-bottom: 1rem;"
              @keyup.enter="handleLogin"
        />
        
        <div v-if="error" style="color: #da1e28; margin-bottom: 1rem;">
          {{ error }}
        </div>
        
        <cv-button @click="handleLogin" :disabled="loading">
          {{ loading ? 'Logging in...' : 'Login' }}
        </cv-button>
      </cv-tile>
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
