<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import '@carbon/web-components/es/components/button/index.js'

const router = useRouter()
const homePageId = ref<string>('')
const loading = ref(true)

onMounted(async () => {
    try {
        const response = await fetch('/api/settings/static')
        if (response.ok) {
            const staticSettings = await response.json()
            homePageId.value = staticSettings.HomePageId || ''

            // If home page is configured, redirect to it
            if (homePageId.value && homePageId.value !== '' && homePageId.value !== 'null') {
                router.push({ name: 'page', params: { id: homePageId.value } })
                return
            }
        }
    } catch (error) {
        console.error('Error fetching home page setting:', error)
    } finally {
        loading.value = false
    }
})

function goToAdmin() {
    router.push('/admin')
}
</script>

<template>
    <div class="home-view">
        <div v-if="loading" class="loading">
            Loading...
        </div>
        <div v-else class="no-home-page">
            <h1>Welcome to PBI Hoster</h1>
            <p>No home page has been configured yet.</p>
            <p>Please configure a home page in the admin settings.</p>
            <cds-button @click="goToAdmin">Go to Admin Panel</cds-button>
        </div>
    </div>
</template>

<style scoped lang="scss">
.home-view {
    display: flex;
    align-items: center;
    justify-content: center;
    min-height: 60vh;
    padding: 2rem;
}

.loading,
.no-home-page {
    text-align: center;
    max-width: 600px;
}

.no-home-page {
    h1 {
        font-size: 2rem;
        margin-bottom: 1rem;
        color: var(--cds-text-primary);
    }

    p {
        margin-bottom: 1rem;
        color: var(--cds-text-secondary);
        font-size: 1.125rem;
    }

    cds-button {
        margin-top: 1rem;
    }
}
</style>
