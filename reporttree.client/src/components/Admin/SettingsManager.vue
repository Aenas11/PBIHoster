<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { api } from '../../services/api'
import { useToastStore } from '../../stores/toast'
import { Add20, Edit20, TrashCan20 } from '@carbon/icons-vue'
import '@carbon/web-components/es/components/button/index.js'
import '@carbon/web-components/es/components/data-table/index.js'
import '@carbon/web-components/es/components/modal/index.js'
import '@carbon/web-components/es/components/text-input/index.js'
import '@carbon/web-components/es/components/select/index.js'
import '@carbon/web-components/es/components/textarea/index.js'
import '@carbon/web-components/es/components/toggle/index.js'

interface Setting {
    key: string
    value: string
    category: string
    description: string
    isEncrypted: boolean
}

interface Page {
    id: number
    title: string
    icon?: string
    parentId?: number
}

const toastStore = useToastStore()
const settings = ref<Setting[]>([])
const pages = ref<Page[]>([])
const loading = ref(false)
const showModal = ref(false)
const isEditing = ref(false)

// Static app settings
const homePageId = ref<string>('')
const demoModeEnabled = ref(false)

const formData = ref<Setting>({
    key: '',
    value: '',
    category: 'General',
    description: '',
    isEncrypted: false
})

const categories = ['General', 'Security', 'PowerBI', 'Email', 'Authentication', 'Application']

// Filter out static app settings from regular settings
const regularSettings = computed(() =>
    settings.value.filter(s => !s.key.startsWith('App.'))
)

// Filter pages to only show bottom-level pages (no parent) or pages without children. this is currently not working properly
const topLevelPages = computed(() =>
    pages.value.filter(page => {
        const hasNoParent = !page.parentId
        const hasNoChildren = !pages.value.some(p => p.parentId === page.id)
        return hasNoParent || hasNoChildren
    })

)

async function loadSettings() {
    loading.value = true
    try {
        settings.value = await api.get<Setting[]>('/settings')
        // Load static app settings
        const homePageSetting = settings.value.find(s => s.key === 'App.HomePageId')
        if (homePageSetting) {
            homePageId.value = homePageSetting.value || ''
        } else {
            homePageId.value = ''
        }

        const demoModeSetting = settings.value.find(s => s.key === 'App.DemoModeEnabled')
        if (demoModeSetting) {
            demoModeEnabled.value = demoModeSetting.value?.toLowerCase() === 'true'
        } else {
            demoModeEnabled.value = false
        }
    } catch (error) {
        console.error('Failed to load settings:', error)
    } finally {
        loading.value = false
    }
}

async function loadPages() {
    try {
        pages.value = await api.get<Page[]>('/pages')
    } catch (error) {
        console.error('Failed to load pages:', error)
    }
}

async function saveStaticSettings() {
    try {
        const payloads = [
            {
                key: 'App.HomePageId',
                value: homePageId.value || '',
                category: 'Application',
                description: 'The page ID to display on the home route (/)'
            },
            {
                key: 'App.DemoModeEnabled',
                value: demoModeEnabled.value.toString(),
                category: 'Application',
                description: 'Enable safe demo pages and sample dataset links'
            }
        ]

        await Promise.all(payloads.map(payload => api.put('/settings', payload)))
        toastStore.success('Success', 'Static settings saved successfully')
        await loadSettings()
    } catch (error) {
        console.error('Failed to save static settings:', error)
        toastStore.error('Error', 'Failed to save static settings')
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
function onHomePageChange(e: CustomEvent<{ value: string }> | Event) {
    // Carbon web components use detail.value or target.value
    homePageId.value = (e as CustomEvent<{ value: string }>).detail?.value || (e.target as HTMLSelectElement)?.value || ''
}
function onDemoModeToggle(e: CustomEvent) {
    const value = (e.detail?.checked ?? e.detail?.value) as boolean | undefined
    demoModeEnabled.value = value ?? false
}

onMounted(() => {
    loadSettings()
    loadPages()
})
</script>

<template>
    <div class="settings-manager">
        <div class="header">
            <h2>Application Settings</h2>
        </div>

        <!-- Static App Settings Section -->
        <section class="static-settings-section">
            <h3>Static Application Settings</h3>
            <p class="section-description">Configure core application behavior and defaults</p>

        <div class="static-settings-form">
            <div class="setting-row">
                <label for="home-page-select">Home Page</label>
                <div class="setting-input">
                    <cds-select id="home-page-select" label-text="Select the page to display on the home route (/)"
                        :value="homePageId" @cds-select-selected="onHomePageChange">
                        <cds-select-item value="">No home page (default)</cds-select-item>
                        <cds-select-item v-for="page in topLevelPages" :key="page.id" :value="page.id.toString()">
                            {{ page.title }}
                        </cds-select-item>
                    </cds-select>

                </div>
            </div>

            <div class="setting-row">
                <label for="demo-toggle">Demo Mode</label>
                <div class="setting-input">
                    <cds-toggle id="demo-toggle" :checked="demoModeEnabled" @cds-toggle-changed="onDemoModeToggle">
                        Enable demo mode to preload sample pages and link the starter dataset without tenant data
                    </cds-toggle>
                    <div class="hint">
                        Starter assets: <a href="/sample-data/sample-sales.csv" target="_blank">sample-sales.csv</a> and
                        <a href="/onboarding/sample-report.svg" target="_blank">sample report preview</a>.
                    </div>
                </div>
            </div>

            <!-- save button -->
                    <cds-button size="sm" @click="saveStaticSettings" style="margin-top: 1rem;">
                        Save Static Settings
                    </cds-button>
        </div>
    </section>

        <!-- Regular Settings Section -->
        <section class="regular-settings-section">
            <div class="header">
                <h3>Advanced Settings</h3>
                <cds-button @click="openCreateModal">
                    <Add20 slot="icon" />
                    Add Setting
                </cds-button>
            </div>

            <cds-table v-if="regularSettings.length > 0">
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
                    <cds-table-row v-for="setting in regularSettings" :key="setting.key">
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
        </section>

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


.header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 2rem;

    h2,
    h3 {
        margin: 0;
        font-size: 1.5rem;
        color: var(--cds-text-primary);
    }

    h3 {
        font-size: 1.25rem;
    }
}

.static-settings-section {
    background: var(--cds-layer-01);
    padding: 1.5rem;
    border-radius: 8px;
    margin-bottom: 2rem;
    border: 1px solid var(--cds-border-subtle-01);

    h3 {
        margin: 0 0 0.5rem 0;
        font-size: 1.25rem;
        color: var(--cds-text-primary);
    }

    .section-description {
        margin: 0 0 1.5rem 0;
        color: var(--cds-text-secondary);
        font-size: 0.875rem;
    }
}

.static-settings-form {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
}

.setting-row {
    display: grid;
    grid-template-columns: 200px 1fr;
    gap: 1rem;
    align-items: start;

    label {
        font-weight: 500;
        color: var(--cds-text-primary);
        padding-top: 0.5rem;
    }

    .setting-input {
        display: flex;
        flex-direction: column;
    }

    .hint {
        color: var(--cds-text-secondary);
        font-size: 0.875rem;
        margin-top: 0.5rem;
    }
}

.regular-settings-section {
    margin-top: 2rem;
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
