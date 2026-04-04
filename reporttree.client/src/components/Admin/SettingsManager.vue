<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { api } from '../../services/api'
import { useToastStore } from '../../stores/toast'
import { useStaticSettingsStore } from '../../stores/staticSettings'
import { Add20, Edit20, TrashCan20 } from '@carbon/icons-vue'
import ThemeManager from '../ThemeManager.vue'
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

interface ExternalGroupMapping {
    externalGroup: string
    internalGroup: string
}

interface ExternalRoleMapping {
    externalRole: string
    internalRole: string
}

interface ExternalAuthProviderResponse {
    providerId: string
    displayName: string
    enabled: boolean
    defaultRole: string
    groupSyncEnabled: boolean
    groupClaimType: string
    removeUnmappedGroupMemberships: boolean
    groupMappings: ExternalGroupMapping[]
    roleSyncEnabled: boolean
    roleClaimType: string
    roleMappings: ExternalRoleMapping[]
}

interface ExternalAuthProviderAdmin extends ExternalAuthProviderResponse {
    groupMappingsText: string
    roleMappingsText: string
}

const toastStore = useToastStore()
const staticSettingsStore = useStaticSettingsStore()
const settings = ref<Setting[]>([])
const pages = ref<Page[]>([])
const externalAuthProviders = ref<ExternalAuthProviderAdmin[]>([])
const externalAuthLoading = ref(false)
const loading = ref(false)
const showModal = ref(false)
const isEditing = ref(false)

// Static app settings
const homePageId = ref<string>('')
const demoModeEnabled = ref(false)
const appName = ref('ReportTree')
const footerText = ref('')
const footerLinkUrl = ref('')
const footerLinkLabel = ref('')

const logoFile = ref<File | null>(null)
const faviconFile = ref<File | null>(null)
const logoInput = ref<HTMLInputElement | null>(null)
const faviconInput = ref<HTMLInputElement | null>(null)
const logoUrl = computed(() => staticSettingsStore.logoUrl)
const faviconUrl = computed(() => staticSettingsStore.faviconUrl)

const formData = ref<Setting>({
    key: '',
    value: '',
    category: 'General',
    description: '',
    isEncrypted: false
})

const categories = ['General', 'Security', 'PowerBI', 'Email', 'Authentication', 'Application', 'Branding']

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

        const appNameSetting = settings.value.find(s => s.key === 'Branding.AppName')
        appName.value = appNameSetting?.value || staticSettingsStore.appName || 'ReportTree'

        const footerTextSetting = settings.value.find(s => s.key === 'Branding.FooterText')
        footerText.value = footerTextSetting?.value || ''

        const footerLinkUrlSetting = settings.value.find(s => s.key === 'Branding.FooterLinkUrl')
        footerLinkUrl.value = footerLinkUrlSetting?.value || ''

        const footerLinkLabelSetting = settings.value.find(s => s.key === 'Branding.FooterLinkLabel')
        footerLinkLabel.value = footerLinkLabelSetting?.value || ''
    } catch (error) {
        console.error('Failed to load settings:', error)
    } finally {
        loading.value = false
    }
}

async function loadExternalAuthProviders() {
    externalAuthLoading.value = true
    try {
        const providers = await api.get<ExternalAuthProviderResponse[]>('/settings/external-auth/providers')
        externalAuthProviders.value = providers.map(provider => ({
            ...provider,
            groupMappingsText: provider.groupMappings
                .map(mapping => `${mapping.externalGroup}=${mapping.internalGroup}`)
                .join('\n'),
            roleMappingsText: provider.roleMappings
                .map(mapping => `${mapping.externalRole}=${mapping.internalRole}`)
                .join('\n')
        }))
    } catch (error) {
        console.error('Failed to load external auth providers:', error)
        externalAuthProviders.value = []
    } finally {
        externalAuthLoading.value = false
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
            },
            {
                key: 'Branding.AppName',
                value: appName.value || 'ReportTree',
                category: 'Branding',
                description: 'Application display name'
            },
            {
                key: 'Branding.FooterText',
                value: footerText.value || '',
                category: 'Branding',
                description: 'Footer text override'
            },
            {
                key: 'Branding.FooterLinkUrl',
                value: footerLinkUrl.value || '',
                category: 'Branding',
                description: 'Footer link URL'
            },
            {
                key: 'Branding.FooterLinkLabel',
                value: footerLinkLabel.value || '',
                category: 'Branding',
                description: 'Footer link label'
            }
        ]

        await Promise.all(payloads.map(payload => api.put('/settings', payload)))
        toastStore.success('Success', 'Static settings saved successfully')
        await loadSettings()
        await staticSettingsStore.load()
    } catch (error) {
        console.error('Failed to save static settings:', error)
        toastStore.error('Error', 'Failed to save static settings')
    }
}

function parsePairLines(text: string): Array<{ left: string, right: string }> {
    return text
        .split('\n')
        .map(line => line.trim())
        .filter(line => line.length > 0)
        .map(line => {
            const separator = line.indexOf('=')
            if (separator <= 0 || separator === line.length - 1) {
                return { left: '', right: '' }
            }

            return {
                left: line.slice(0, separator).trim(),
                right: line.slice(separator + 1).trim()
            }
        })
        .filter(pair => pair.left.length > 0 && pair.right.length > 0)
}

function onExternalDefaultRoleChange(providerId: string, event: Event) {
    const value = (event.target as HTMLSelectElement).value
    const provider = externalAuthProviders.value.find(p => p.providerId === providerId)
    if (provider) {
        provider.defaultRole = value
    }
}

function onExternalGroupSyncToggle(providerId: string, event: CustomEvent) {
    const provider = externalAuthProviders.value.find(p => p.providerId === providerId)
    if (!provider) {
        return
    }

    provider.groupSyncEnabled = Boolean(event.detail?.checked ?? event.detail?.value)
}

function onExternalGroupClaimTypeInput(providerId: string, event: Event) {
    const provider = externalAuthProviders.value.find(p => p.providerId === providerId)
    if (provider) {
        provider.groupClaimType = (event.target as HTMLInputElement).value
    }
}

function onExternalRemoveUnmappedToggle(providerId: string, event: CustomEvent) {
    const provider = externalAuthProviders.value.find(p => p.providerId === providerId)
    if (!provider) {
        return
    }

    provider.removeUnmappedGroupMemberships = Boolean(event.detail?.checked ?? event.detail?.value)
}

function onExternalGroupMappingsInput(providerId: string, event: Event) {
    const provider = externalAuthProviders.value.find(p => p.providerId === providerId)
    if (provider) {
        provider.groupMappingsText = (event.target as HTMLTextAreaElement).value
    }
}

function onExternalRoleSyncToggle(providerId: string, event: CustomEvent) {
    const provider = externalAuthProviders.value.find(p => p.providerId === providerId)
    if (!provider) {
        return
    }

    provider.roleSyncEnabled = Boolean(event.detail?.checked ?? event.detail?.value)
}

function onExternalRoleClaimTypeInput(providerId: string, event: Event) {
    const provider = externalAuthProviders.value.find(p => p.providerId === providerId)
    if (provider) {
        provider.roleClaimType = (event.target as HTMLInputElement).value
    }
}

function onExternalRoleMappingsInput(providerId: string, event: Event) {
    const provider = externalAuthProviders.value.find(p => p.providerId === providerId)
    if (provider) {
        provider.roleMappingsText = (event.target as HTMLTextAreaElement).value
    }
}

async function saveExternalAuthMappings() {
    try {
        const payload = {
            providers: externalAuthProviders.value.map(provider => ({
                providerId: provider.providerId,
                defaultRole: provider.defaultRole,
                groupSyncEnabled: provider.groupSyncEnabled,
                groupClaimType: provider.groupClaimType || 'groups',
                removeUnmappedGroupMemberships: provider.removeUnmappedGroupMemberships,
                groupMappings: parsePairLines(provider.groupMappingsText).map(pair => ({
                    externalGroup: pair.left,
                    internalGroup: pair.right
                })),
                roleSyncEnabled: provider.roleSyncEnabled,
                roleClaimType: provider.roleClaimType || 'roles',
                roleMappings: parsePairLines(provider.roleMappingsText).map(pair => ({
                    externalRole: pair.left,
                    internalRole: pair.right
                }))
            }))
        }

        await api.put('/settings/external-auth/providers', payload)
        toastStore.success('Success', 'External auth mapping settings saved')
        await loadExternalAuthProviders()
    } catch (error) {
        console.error('Failed to save external auth mappings:', error)
        toastStore.error('Error', 'Failed to save external auth mappings')
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
function onAppNameInput(e: Event) { appName.value = (e.target as HTMLInputElement).value }
function onFooterTextInput(e: Event) { footerText.value = (e.target as HTMLInputElement).value }
function onFooterLinkUrlInput(e: Event) { footerLinkUrl.value = (e.target as HTMLInputElement).value }
function onFooterLinkLabelInput(e: Event) { footerLinkLabel.value = (e.target as HTMLInputElement).value }
function onLogoFileChange(e: Event) {
    logoFile.value = (e.target as HTMLInputElement).files?.[0] ?? null
}
function onFaviconFileChange(e: Event) {
    faviconFile.value = (e.target as HTMLInputElement).files?.[0] ?? null
}

function getAuthHeader(): HeadersInit {
    const token = localStorage.getItem('token')
    return token ? { Authorization: `Bearer ${token}` } : {}
}

async function uploadBrandingAsset(assetType: 'logo' | 'favicon', file: File | null) {
    if (!file) {
        toastStore.error('Error', 'Select a file to upload')
        return
    }

    const formData = new FormData()
    formData.append('file', file)

    const response = await fetch(`/api/branding/assets/${assetType}`, {
        method: 'POST',
        headers: getAuthHeader(),
        body: formData
    })

    if (!response.ok) {
        const error = await response.json().catch(() => ({ error: 'Upload failed' }))
        throw new Error(error.error || 'Upload failed')
    }

    await staticSettingsStore.load()
    await loadSettings()

    if (assetType === 'logo') {
        logoFile.value = null
        if (logoInput.value) logoInput.value.value = ''
    } else {
        faviconFile.value = null
        if (faviconInput.value) faviconInput.value.value = ''
    }
}

async function deleteBrandingAsset(assetType: 'logo' | 'favicon') {
    if (!confirm(`Remove the current ${assetType}?`)) {
        return
    }

    const response = await fetch(`/api/branding/assets/${assetType}`, {
        method: 'DELETE',
        headers: getAuthHeader()
    })

    if (!response.ok) {
        const error = await response.json().catch(() => ({ error: 'Delete failed' }))
        throw new Error(error.error || 'Delete failed')
    }

    await staticSettingsStore.load()
    await loadSettings()
}

async function handleUploadLogo() {
    try {
        await uploadBrandingAsset('logo', logoFile.value)
        toastStore.success('Success', 'Logo updated')
    } catch (error) {
        console.error('Logo upload failed', error)
        toastStore.error('Error', 'Logo upload failed')
    }
}

async function handleUploadFavicon() {
    try {
        await uploadBrandingAsset('favicon', faviconFile.value)
        toastStore.success('Success', 'Favicon updated')
    } catch (error) {
        console.error('Favicon upload failed', error)
        toastStore.error('Error', 'Favicon upload failed')
    }
}

async function handleDeleteLogo() {
    try {
        await deleteBrandingAsset('logo')
        toastStore.success('Success', 'Logo removed')
    } catch (error) {
        console.error('Logo delete failed', error)
        toastStore.error('Error', 'Logo delete failed')
    }
}

async function handleDeleteFavicon() {
    try {
        await deleteBrandingAsset('favicon')
        toastStore.success('Success', 'Favicon removed')
    } catch (error) {
        console.error('Favicon delete failed', error)
        toastStore.error('Error', 'Favicon delete failed')
    }
}

onMounted(() => {
    loadSettings()
    loadPages()
    loadExternalAuthProviders()
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
                            Starter assets: <a href="/sample-data/sample-sales.csv" target="_blank">sample-sales.csv</a>
                            and
                            <a href="/onboarding/sample-report.svg" target="_blank">sample report preview</a>.
                        </div>
                    </div>
                </div>

                <div class="setting-row">
                    <label for="app-name">Application Name</label>
                    <div class="setting-input">
                        <cds-text-input id="app-name" label="" :value="appName" @input="onAppNameInput"
                            placeholder="ReportTree"></cds-text-input>
                    </div>
                </div>

                <div class="setting-row">
                    <label for="footer-text">Footer Text</label>
                    <div class="setting-input">
                        <cds-text-input id="footer-text" label="" :value="footerText" @input="onFooterTextInput"
                            placeholder="Leave blank to use default"></cds-text-input>
                    </div>
                </div>

                <div class="setting-row">
                    <label for="footer-link-url">Footer Link URL</label>
                    <div class="setting-input">
                        <cds-text-input id="footer-link-url" label="" :value="footerLinkUrl"
                            @input="onFooterLinkUrlInput" placeholder="https://example.com"></cds-text-input>
                    </div>
                </div>

                <div class="setting-row">
                    <label for="footer-link-label">Footer Link Label</label>
                    <div class="setting-input">
                        <cds-text-input id="footer-link-label" label="" :value="footerLinkLabel"
                            @input="onFooterLinkLabelInput" placeholder="Support"></cds-text-input>
                    </div>
                </div>

                <div class="setting-row">
                    <label>Brand Logo</label>
                    <div class="setting-input">
                        <div class="asset-row">
                            <img v-if="logoUrl" :src="logoUrl" class="asset-preview" alt="" aria-hidden="true" />
                            <span v-else class="asset-placeholder">No logo uploaded</span>
                        </div>
                        <div class="asset-actions">
                            <input ref="logoInput" type="file" accept="image/png,image/jpeg,image/webp"
                                @change="onLogoFileChange" />
                            <cds-button size="sm" @click="handleUploadLogo">Upload Logo</cds-button>
                            <cds-button size="sm" kind="secondary" @click="handleDeleteLogo"
                                :disabled="!logoUrl">Remove</cds-button>
                        </div>
                        <div class="hint">PNG, JPG, or WebP. Max 2 MB.</div>
                    </div>
                </div>

                <div class="setting-row">
                    <label>Favicon</label>
                    <div class="setting-input">
                        <div class="asset-row">
                            <img v-if="faviconUrl" :src="faviconUrl" class="asset-preview" alt="" aria-hidden="true" />
                            <span v-else class="asset-placeholder">No favicon uploaded</span>
                        </div>
                        <div class="asset-actions">
                            <input ref="faviconInput" type="file"
                                accept="image/png,image/x-icon,image/vnd.microsoft.icon"
                                @change="onFaviconFileChange" />
                            <cds-button size="sm" @click="handleUploadFavicon">Upload Favicon</cds-button>
                            <cds-button size="sm" kind="secondary" @click="handleDeleteFavicon"
                                :disabled="!faviconUrl">Remove</cds-button>
                        </div>
                        <div class="hint">PNG or ICO. Max 2 MB.</div>
                    </div>
                </div>

                <!-- save button -->
                <cds-button size="sm" @click="saveStaticSettings" style="margin-top: 1rem;">
                    Save Static Settings
                </cds-button>
            </div>
        </section>

        <section class="theme-settings-section">
            <h3>Theme Management</h3>
            <p class="section-description">Create and maintain custom Carbon token themes available in the header switcher</p>
            <ThemeManager />
        </section>

        <section class="external-auth-section">
            <h3>External Authentication Mapping</h3>
            <p class="section-description">
                Configure claim-based role and group mappings for external identity providers.
                Provider credentials and OIDC connection details remain config/env-only and are intentionally not editable here.
            </p>

            <div v-if="externalAuthLoading" class="hint">Loading external providers...</div>

            <div v-else-if="externalAuthProviders.length === 0" class="hint">
                No external providers found. Enable providers in server configuration first.
            </div>

            <div v-else class="external-provider-list">
                <div v-for="provider in externalAuthProviders" :key="provider.providerId" class="external-provider-card">
                    <div class="external-provider-header">
                        <h4>{{ provider.displayName }}</h4>
                        <span class="category-badge">{{ provider.enabled ? 'Enabled' : 'Disabled' }}</span>
                    </div>

                    <div class="setting-row compact">
                        <label>Default Role</label>
                        <div class="setting-input">
                            <cds-select :value="provider.defaultRole" label-text="Default fallback role"
                                @change="onExternalDefaultRoleChange(provider.providerId, $event)">
                                <cds-select-item value="Viewer">Viewer</cds-select-item>
                                <cds-select-item value="Editor">Editor</cds-select-item>
                                <cds-select-item value="Admin">Admin</cds-select-item>
                            </cds-select>
                        </div>
                    </div>

                    <div class="setting-row compact">
                        <label>Group Mapping</label>
                        <div class="setting-input">
                            <cds-toggle :checked="provider.groupSyncEnabled"
                                @cds-toggle-changed="onExternalGroupSyncToggle(provider.providerId, $event)">
                                Enable external group to internal group synchronization
                            </cds-toggle>
                            <cds-text-input label="Group claim type" :value="provider.groupClaimType"
                                @input="onExternalGroupClaimTypeInput(provider.providerId, $event)"
                                placeholder="groups"></cds-text-input>
                            <cds-toggle :checked="provider.removeUnmappedGroupMemberships"
                                @cds-toggle-changed="onExternalRemoveUnmappedToggle(provider.providerId, $event)">
                                Remove managed group memberships when claims are no longer present
                            </cds-toggle>
                            <cds-textarea label="Group mappings (ExternalGroup=InternalGroup, one per line)"
                                :value="provider.groupMappingsText"
                                @input="onExternalGroupMappingsInput(provider.providerId, $event)"
                                placeholder="entra-group-id=Sales"></cds-textarea>
                        </div>
                    </div>

                    <div class="setting-row compact">
                        <label>Role Mapping</label>
                        <div class="setting-input">
                            <cds-toggle :checked="provider.roleSyncEnabled"
                                @cds-toggle-changed="onExternalRoleSyncToggle(provider.providerId, $event)">
                                Enable external role to internal role synchronization
                            </cds-toggle>
                            <cds-text-input label="Role claim type" :value="provider.roleClaimType"
                                @input="onExternalRoleClaimTypeInput(provider.providerId, $event)"
                                placeholder="roles"></cds-text-input>
                            <cds-textarea label="Role mappings (ExternalRole=InternalRole, one per line)"
                                :value="provider.roleMappingsText"
                                @input="onExternalRoleMappingsInput(provider.providerId, $event)"
                                placeholder="PBIHoster-Editors=Editor"></cds-textarea>
                        </div>
                    </div>
                </div>

                <cds-button size="sm" @click="saveExternalAuthMappings">Save External Auth Mappings</cds-button>
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

.asset-row {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}

.asset-preview {
    height: 36px;
    width: auto;
}

.asset-placeholder {
    font-size: 0.875rem;
    color: var(--cds-text-secondary);
}

.asset-actions {
    display: flex;
    flex-wrap: wrap;
    gap: 0.75rem;
    margin-top: 0.5rem;
    align-items: center;
}

.regular-settings-section {
    margin-top: 2rem;
}

.theme-settings-section {
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

.external-auth-section {
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
        margin: 0 0 1rem 0;
        color: var(--cds-text-secondary);
        font-size: 0.875rem;
    }
}

.external-provider-list {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.external-provider-card {
    border: 1px solid var(--cds-border-subtle-01);
    border-radius: 8px;
    padding: 1rem;
    background: var(--cds-layer-02);
}

.external-provider-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.75rem;

    h4 {
        margin: 0;
        color: var(--cds-text-primary);
    }
}

.setting-row.compact {
    grid-template-columns: 180px 1fr;
    margin-bottom: 0.75rem;
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
