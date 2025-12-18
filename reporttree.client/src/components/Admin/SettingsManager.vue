<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { api } from '../../services/api'
import { useToastStore } from '../../stores/toast'
import { Add20, Edit20, TrashCan20 } from '@carbon/icons-vue'
import '@carbon/web-components/es/components/button/index.js'
import '@carbon/web-components/es/components/data-table/index.js'
import '@carbon/web-components/es/components/modal/index.js'
import '@carbon/web-components/es/components/text-input/index.js'
import '@carbon/web-components/es/components/select/index.js'
import '@carbon/web-components/es/components/textarea/index.js'

interface Setting {
    key: string
    value: string
    category: string
    description: string
    isEncrypted: boolean
}

const toastStore = useToastStore()
const settings = ref<Setting[]>([])
const loading = ref(false)
const showModal = ref(false)
const isEditing = ref(false)

const formData = ref<Setting>({
    key: '',
    value: '',
    category: 'General',
    description: '',
    isEncrypted: false
})

const categories = ['General', 'Security', 'PowerBI', 'Email', 'Authentication']

async function loadSettings() {
    loading.value = true
    try {
        settings.value = await api.get<Setting[]>('/settings')
    } catch (error) {
        console.error('Failed to load settings:', error)
    } finally {
        loading.value = false
    }
}

function openCreateModal() {
    isEditing.value = false
    formData.value = {
        key: '',
        value: '',
        category: 'General',
        description: '',
        isEncrypted: false
    }
    showModal.value = true
}

function openEditModal(setting: Setting) {
    isEditing.value = true
    formData.value = { ...setting }
    showModal.value = true
}

async function saveSetting() {
    try {
        await api.put('/settings', formData.value)
        toastStore.success('Success', 'Setting saved successfully')
        showModal.value = false
        await loadSettings()
    } catch (error) {
        console.error('Failed to save setting:', error)
    }
}

async function deleteSetting(key: string) {
    if (!confirm(`Are you sure you want to delete the setting "${key}"?`)) {
        return
    }

    try {
        await api.delete(`/settings/${encodeURIComponent(key)}`)
        toastStore.success('Success', 'Setting deleted successfully')
        await loadSettings()
    } catch (error) {
        console.error('Failed to delete setting:', error)
    }
}

function onKeyInput(e: Event) { formData.value.key = (e.target as HTMLInputElement).value }
function onValueInput(e: Event) { formData.value.value = (e.target as HTMLInputElement).value }
function onCategoryChange(e: Event) { formData.value.category = (e.target as HTMLSelectElement).value }
function onDescriptionInput(e: Event) { formData.value.description = (e.target as HTMLTextAreaElement).value }

onMounted(() => {
    loadSettings()
})
</script>

<template>
    <div class="settings-manager">
        <div class="header">
            <h2>Application Settings</h2>
            <cds-button @click="openCreateModal">
                <Add20 slot="icon" />
                Add Setting
            </cds-button>
        </div>

        <cds-table v-if="settings.length > 0">
            <cds-table-head>
                <cds-table-header-row>
                    <cds-table-header-cell>Key</cds-table-header-cell>
                    <cds-table-header-cell>Value</cds-table-header-cell>
                    <cds-table-header-cell>Category</cds-table-header-cell>
                    <cds-table-header-cell>Description</cds-table-header-cell>
                    <cds-table-header-cell>Actions</cds-table-header-cell>
                </cds-table-header-row>
            </cds-table-head>
            <cds-table-body>
                <cds-table-row v-for="setting in settings" :key="setting.key">
                    <cds-table-cell><strong>{{ setting.key }}</strong></cds-table-cell>
                    <cds-table-cell>
                        <code class="value">{{ setting.value }}</code>
                    </cds-table-cell>
                    <cds-table-cell>
                        <span class="category-badge">{{ setting.category }}</span>
                    </cds-table-cell>
                    <cds-table-cell>{{ setting.description || '-' }}</cds-table-cell>
                    <cds-table-cell>
                        <div class="actions">
                            <button class="action-btn" @click="openEditModal(setting)" title="Edit setting">
                                <Edit20 />
                            </button>
                            <button class="action-btn danger" @click="deleteSetting(setting.key)"
                                title="Delete setting">
                                <TrashCan20 />
                            </button>
                        </div>
                    </cds-table-cell>
                </cds-table-row>
            </cds-table-body>
        </cds-table>

        <cds-modal :open="showModal" @cds-modal-closed="showModal = false">
            <cds-modal-header>
                <cds-modal-close-button></cds-modal-close-button>
                <cds-modal-label>{{ isEditing ? 'Edit Setting' : 'New Setting' }}</cds-modal-label>
                <cds-modal-heading>{{ isEditing ? formData.key : 'Create New Setting' }}</cds-modal-heading>
            </cds-modal-header>
            <cds-modal-body>
                <cds-text-input label="Key" :value="formData.key" @input="onKeyInput" :disabled="isEditing"
                    placeholder="e.g., PowerBI.ClientId"></cds-text-input>
                <br />
                <cds-text-input label="Value" :value="formData.value" @input="onValueInput"
                    placeholder="Setting value"></cds-text-input>
                <br />
                <cds-select label-text="Category" :value="formData.category" @change="onCategoryChange">
                    <cds-select-item v-for="cat in categories" :key="cat" :value="cat">{{ cat }}</cds-select-item>
                </cds-select>
                <br />
                <cds-textarea label="Description" :value="formData.description" @input="onDescriptionInput"
                    placeholder="Describe what this setting does"></cds-textarea>
            </cds-modal-body>
            <cds-modal-footer>
                <cds-modal-footer-button kind="secondary" @click="showModal = false">Cancel</cds-modal-footer-button>
                <cds-modal-footer-button kind="primary" @click="saveSetting">Save</cds-modal-footer-button>
            </cds-modal-footer>
        </cds-modal>
    </div>
</template>

<style scoped lang="scss">
.settings-manager {
    padding: 2rem;
}

.header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 2rem;

    h2 {
        margin: 0;
        font-size: 1.5rem;
        color: var(--cds-text-primary);
    }
}

.value {
    font-family: 'IBM Plex Mono', monospace;
    font-size: 0.875rem;
    padding: 0.25rem 0.5rem;
    background: var(--cds-layer-02);
    border-radius: 4px;
    max-width: 200px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    display: inline-block;
}

.category-badge {
    display: inline-block;
    padding: 0.25rem 0.5rem;
    background: var(--cds-layer-accent-01);
    color: var(--cds-text-primary);
    border-radius: 4px;
    font-size: 0.75rem;
    font-weight: 500;
}

.actions {
    display: flex;
    gap: 0.5rem;
}

.action-btn {
    background: none;
    border: none;
    padding: 0.5rem;
    cursor: pointer;
    color: var(--cds-icon-primary);
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 4px;

    &:hover {
        background: var(--cds-layer-hover-01);
    }

    &.danger {
        color: var(--cds-support-error);

        &:hover {
            background: var(--cds-support-error-hover);
        }
    }
}

.form {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    margin-top: 1rem;
}
</style>
