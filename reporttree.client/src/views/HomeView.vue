<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import '@carbon/web-components/es/components/button/index.js'
import { useStaticSettingsStore } from '@/stores/staticSettings'

const router = useRouter()
const staticSettings = useStaticSettingsStore()
const loading = ref(true)

onMounted(async () => {
    try {
        if (!staticSettings.isLoaded) {
            await staticSettings.load()
        }

        // If home page is configured, redirect to it
        if (staticSettings.homePageId && staticSettings.homePageId !== '' && staticSettings.homePageId !== 'null') {
            router.push({ name: 'page', params: { id: staticSettings.homePageId } })
            return
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
            <p v-if="staticSettings.demoModeEnabled" class="demo-banner">
                Demo mode is enabled — start from the <strong>Demo Overview</strong> page in the side navigation to see
                sample layouts and the quick-start dataset.
            </p>
            <cds-button @click="goToAdmin">Go to Admin Panel</cds-button>
            <cds-button kind="tertiary" href="/help" style="margin-left: 0.5rem;">View Help &amp;
                Walkthroughs</cds-button>
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

.demo-banner {
    background: rgba(15, 98, 254, 0.08);
    border: 1px solid rgba(15, 98, 254, 0.3);
    padding: 0.75rem;
    border-radius: 8px;
    margin: 1rem 0;
}
</style>
