<script setup lang="ts">
import { ref, computed } from 'vue'
import { useThemeStore } from '@/stores/theme'
import { useAuthStore } from '@/stores/auth'
import type { CustomTheme } from '@/stores/theme'
import { Add20, TrashCan20 } from '@carbon/icons-vue'

import '@carbon/web-components/es/components/button/index.js';
import '@carbon/web-components/es/components/data-table/index.js';
import '@carbon/web-components/es/components/modal/index.js';
import '@carbon/web-components/es/components/text-input/index.js';
import '@carbon/web-components/es/components/textarea/index.js';
import '@carbon/web-components/es/components/accordion/index.js';

const themeStore = useThemeStore()
const authStore = useAuthStore()

const showModal = ref(false)
const editingTheme = ref<CustomTheme | null>(null)
const themeName = ref('')
const organizationId = ref('')
const themeTokens = ref('')

const canManageThemes = computed(() => {
    return authStore.roles.includes('Admin') || authStore.roles.includes('Editor')
})

function openCreateModal() {
    editingTheme.value = null
    themeName.value = ''
    organizationId.value = ''
    themeTokens.value = ''
    showModal.value = true
}

function openEditModal(theme: CustomTheme) {
    editingTheme.value = theme
    themeName.value = theme.name
    organizationId.value = theme.organizationId || ''
    themeTokens.value = JSON.stringify(theme.tokens, null, 2)
    showModal.value = true
}

async function saveTheme() {
    try {
        let tokens: Record<string, string>
        try {
            tokens = JSON.parse(themeTokens.value)
        } catch {
            alert('Invalid JSON format for theme tokens')
            return
        }

        if (editingTheme.value) {
            // Update existing theme
            await themeStore.saveCustomTheme({
                name: themeName.value,
                tokens,
                organizationId: organizationId.value || undefined,
                isCustom: true
            })
        } else {
            // Create new theme
            await themeStore.saveCustomTheme({
                name: themeName.value,
                tokens,
                organizationId: organizationId.value || undefined,
                isCustom: true
            })
        }

        showModal.value = false
        await themeStore.loadCustomThemes()
    } catch (error) {
        console.error('Failed to save theme:', error)
        alert('Failed to save theme')
    }
}

async function deleteTheme(themeId: string) {
    if (confirm('Are you sure you want to delete this theme?')) {
        try {
            await themeStore.deleteCustomTheme(themeId)
        } catch (error) {
            console.error('Failed to delete theme:', error)
            alert('Failed to delete theme')
        }
    }
}

const sampleThemeJson = `{
  "background": "#ffffff",
  "interactive-01": "#0f62fe",
  "interactive-02": "#393939",
  "interactive-03": "#0f62fe",
  "interactive-04": "#0f62fe",
  "ui-background": "#ffffff",
  "ui-01": "#f4f4f4",
  "ui-02": "#ffffff",
  "ui-03": "#e0e0e0",
  "ui-04": "#8d8d8d",
  "ui-05": "#161616",
  "text-01": "#161616",
  "text-02": "#525252",
  "text-03": "#a8a8a8",
  "text-04": "#ffffff",
  "text-error": "#da1e28",
  "icon-01": "#161616",
  "icon-02": "#525252",
  "icon-03": "#ffffff",
  "link-01": "#0f62fe",
  "field-01": "#f4f4f4",
  "field-02": "#ffffff"
}`
</script>

<template>
    <div v-if="canManageThemes" class="theme-manager">
        <cds-button @click="openCreateModal" kind="primary">
            Create Custom Theme
            <Add20 slot="icon" />
        </cds-button>

        <cds-table v-if="themeStore.customThemes.length > 0" class="theme-table">
            <cds-table-head>
                <cds-table-header-row>
                    <cds-table-header-cell>Name</cds-table-header-cell>
                    <cds-table-header-cell>Organization</cds-table-header-cell>
                    <cds-table-header-cell>Created By</cds-table-header-cell>
                    <cds-table-header-cell>Actions</cds-table-header-cell>
                </cds-table-header-row>
            </cds-table-head>
            <cds-table-body>
                <cds-table-row v-for="theme in themeStore.customThemes" :key="theme.id">
                    <cds-table-cell>{{ theme.name }}</cds-table-cell>
                    <cds-table-cell>{{ theme.organizationId || 'Global' }}</cds-table-cell>
                    <cds-table-cell>{{ theme.createdBy }}</cds-table-cell>
                    <cds-table-cell>
                        <cds-button kind="ghost" size="sm" @click="openEditModal(theme)">
                            Edit
                        </cds-button>
                        <cds-button kind="danger-ghost" size="sm" @click="deleteTheme(theme.id)">
                            <TrashCan20 slot="icon" />
                        </cds-button>
                    </cds-table-cell>
                </cds-table-row>
            </cds-table-body>
        </cds-table>

        <cds-modal :open="showModal" @cds-modal-closed="showModal = false">
            <cds-modal-header>
                <cds-modal-close-button></cds-modal-close-button>
                <cds-modal-label>Custom Theme</cds-modal-label>
                <cds-modal-heading>{{ editingTheme ? 'Edit' : 'Create' }} Custom Theme</cds-modal-heading>
            </cds-modal-header>
            <cds-modal-body>
                <cds-text-input :value="themeName" @input="themeName = ($event.target as HTMLInputElement).value"
                    label="Theme Name" placeholder="Enter theme name" class="theme-input">
                </cds-text-input>

                <cds-text-input :value="organizationId"
                    @input="organizationId = ($event.target as HTMLInputElement).value"
                    label="Organization ID (Optional)" placeholder="Leave empty for global theme" class="theme-input">
                </cds-text-input>

                <cds-textarea :value="themeTokens" @input="themeTokens = ($event.target as HTMLTextAreaElement).value"
                    label="Theme Tokens (JSON)" placeholder="Enter theme tokens as JSON" rows="15" class="theme-input">
                </cds-textarea>

                <cds-accordion>
                    <cds-accordion-item title="Sample Theme JSON">
                        <pre class="sample-json">{{ sampleThemeJson }}</pre>
                    </cds-accordion-item>
                </cds-accordion>

                <p class="theme-help">
                    For a complete list of Carbon theme tokens, visit:
                    <a href="https://carbondesignsystem.com/elements/color/tokens/" target="_blank">
                        Carbon Design System Color Tokens
                    </a>
                </p>
            </cds-modal-body>
            <cds-modal-footer>
                <cds-modal-footer-button kind="secondary" @click="showModal = false">Cancel</cds-modal-footer-button>
                <cds-modal-footer-button kind="primary" :disabled="!themeName || !themeTokens"
                    @click="saveTheme">Save</cds-modal-footer-button>
            </cds-modal-footer>
        </cds-modal>
    </div>
</template>

<style scoped>
.theme-manager {
    padding: 2rem;
}

.theme-table {
    margin-top: 2rem;
}

.theme-input {
    margin-bottom: 1rem;
}

.sample-json {
    background: var(--cds-field-01);
    padding: 1rem;
    border-radius: 4px;
    overflow-x: auto;
    font-size: 0.875rem;
}

.theme-help {
    margin-top: 1rem;
    font-size: 0.875rem;
    color: var(--cds-text-02);
}

.theme-help a {
    color: var(--cds-link-01);
    text-decoration: none;
}

.theme-help a:hover {
    text-decoration: underline;
}
</style>
