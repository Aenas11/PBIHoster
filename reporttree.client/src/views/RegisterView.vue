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
const confirmPassword = ref('')
const error = ref('')
const loading = ref(false)

async function handleRegister() {
    if (password.value !== confirmPassword.value) {
        error.value = 'Passwords do not match'
        return
    }

    loading.value = true
    error.value = ''

    try {
        await auth.register(username.value, password.value)
        // Redirect to login after successful registration
        router.push('/login')
    } catch (e: unknown) {
        // Handle API error response structure
        if (e && typeof e === 'object' && 'response' in e) {
            const apiError = e as { response?: { data?: { errors?: string[] } } }
            if (apiError.response?.data?.errors) {
                error.value = apiError.response.data.errors.join(', ')
            } else {
                error.value = 'Registration failed'
            }
        } else if (e instanceof Error) {
            error.value = e.message
        } else {
            error.value = 'Registration failed'
        }
    } finally {
        loading.value = false
    }
}
</script>

<template>
    <div class="login-container">
        <div class="login-card">
            <cds-tile>
                <h2 style="margin-bottom: 1rem;">Register</h2>

                <cds-text-input :value="username" @input="username = ($event.target as HTMLInputElement).value"
                    label="Username" placeholder="Enter username" style="margin-bottom: 1rem;"></cds-text-input>

                <cds-text-input :value="password" @input="password = ($event.target as HTMLInputElement).value"
                    label="Password" type="password" placeholder="Enter password"
                    style="margin-bottom: 1rem;"></cds-text-input>

                <cds-text-input :value="confirmPassword"
                    @input="confirmPassword = ($event.target as HTMLInputElement).value" label="Confirm Password"
                    type="password" placeholder="Confirm password" style="margin-bottom: 1rem;"
                    @keyup.enter="handleRegister"></cds-text-input>

                <div v-if="error" style="color: #da1e28; margin-bottom: 1rem;">
                    {{ error }}
                </div>

                <div style="display: flex; justify-content: space-between; align-items: center;">
                    <cds-link href="/login">Back to Login</cds-link>
                    <cds-button @click="handleRegister" :disabled="loading">
                        {{ loading ? 'Registering...' : 'Register' }}
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
