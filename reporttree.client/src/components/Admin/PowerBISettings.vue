<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { api } from '../../services/api'
import { useToastStore } from '../../stores/toast'
import '@carbon/web-components/es/components/button/index.js'
import '@carbon/web-components/es/components/text-input/index.js'
import '@carbon/web-components/es/components/select/index.js'
import '@carbon/web-components/es/components/form-group/index.js'

const toastStore = useToastStore()
const loading = ref(false)

const config = ref({
    authType: 'ServicePrincipal',
    tenantId: '',
    clientId: '',
    clientSecret: '',
    authorityUrl: 'https://login.microsoftonline.com/common/',
    resourceUrl: 'https://analysis.windows.net/powerbi/api',
    apiUrl: 'https://api.powerbi.com'
})

const authTypes = [
    { value: 'ServicePrincipal', text: 'Service Principal' },
    { value: 'MasterUser', text: 'Master User' }
]

async function loadSettings() {
    loading.value = true
    try {
        const settings = await api.get<any[]>('/settings')
        const getVal = (key: string) => settings.find(s => s.key === key)?.value || ''

        config.value.authType = getVal('PowerBI.AuthType') || 'ServicePrincipal'
        config.value.tenantId = getVal('PowerBI.TenantId')
        config.value.clientId = getVal('PowerBI.ClientId')
        config.value.clientSecret = getVal('PowerBI.ClientSecret')
        config.value.authorityUrl = getVal('PowerBI.AuthorityUrl') || 'https://login.microsoftonline.com/common/'
        config.value.resourceUrl = getVal('PowerBI.ResourceUrl') || 'https://analysis.windows.net/powerbi/api'
        config.value.apiUrl = getVal('PowerBI.ApiUrl') || 'https://api.powerbi.com'
    } catch (error) {
        console.error('Failed to load Power BI settings:', error)
        toastStore.error('Error', 'Failed to load settings')
    } finally {
        loading.value = false
    }
}

async function saveSettings() {
    loading.value = true
    try {
        const saves = [
            { key: 'PowerBI.AuthType', value: config.value.authType },
            { key: 'PowerBI.TenantId', value: config.value.tenantId },
            { key: 'PowerBI.ClientId', value: config.value.clientId },
            { key: 'PowerBI.ClientSecret', value: config.value.clientSecret, isEncrypted: true },
            { key: 'PowerBI.AuthorityUrl', value: config.value.authorityUrl },
            { key: 'PowerBI.ResourceUrl', value: config.value.resourceUrl },
            { key: 'PowerBI.ApiUrl', value: config.value.apiUrl }
        ]

        for (const s of saves) {
            await api.put('/settings', {
                key: s.key,
                value: s.value,
                category: 'PowerBI',
                isEncrypted: s.isEncrypted || false
            })
        }

        toastStore.success('Success', 'Power BI settings saved')
    } catch (error) {
        console.error('Failed to save settings:', error)
        toastStore.error('Error', 'Failed to save settings')
    } finally {
        loading.value = false
    }
}

onMounted(() => {
    loadSettings()
})
</script>

<template>
    <div class="pbi-settings">
        <h3>Power BI Configuration</h3>
        <div class="form-container">
            <cds-select label-text="Authentication Type" :value="config.authType"
                @cds-select-selected="config.authType = $event.target.value">
                <cds-select-item v-for="type in authTypes" :key="type.value" :value="type.value">
                    {{ type.text }}
                </cds-select-item>
            </cds-select>

            <cds-text-input label="Tenant ID" placeholder="Enter Tenant ID" :value="config.tenantId"
                @input="config.tenantId = ($event.target as HTMLInputElement).value"></cds-text-input>

            <cds-text-input label="Client ID" placeholder="Enter Client ID" :value="config.clientId"
                @input="config.clientId = ($event.target as HTMLInputElement).value"></cds-text-input>

            <cds-text-input label="Client Secret" type="password" placeholder="Enter Client Secret"
                :value="config.clientSecret"
                @input="config.clientSecret = ($event.target as HTMLInputElement).value"></cds-text-input>

            <cds-text-input label="Authority URL" :value="config.authorityUrl"
                @input="config.authorityUrl = ($event.target as HTMLInputElement).value"></cds-text-input>

            <div class="actions">
                <cds-button @click="saveSettings" :disabled="loading">
                    {{ loading ? 'Saving...' : 'Save Configuration' }}
                </cds-button>
            </div>
        </div>
    </div>
</template>
<style scoped>
.pbi-settings {
    padding: 1rem;
    max-width: 600px;
}

.form-container {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
    margin-top: 1.5rem;
}

.actions {
    margin-top: 1rem;
}
</style>