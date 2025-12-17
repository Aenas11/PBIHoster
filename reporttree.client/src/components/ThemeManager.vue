<script setup lang="ts">
import { ref, computed } from 'vue'
import { useThemeStore } from '@/stores/theme'
import { useAuthStore } from '@/stores/auth'
import type { CustomTheme } from '@/stores/theme'
import { Add20, TrashCan20 } from '@carbon/icons-vue'

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
        <cv-button @click="openCreateModal" kind="primary">
            <Add20 />
            Create Custom Theme
        </cv-button>

        <cv-data-table v-if="themeStore.customThemes.length > 0"
            :columns="['Name', 'Organization', 'Created By', 'Actions']" class="theme-table">
            <template v-slot:data>
                <cv-data-table-row v-for="theme in themeStore.customThemes" :key="theme.id">
                    <cv-data-table-cell>{{ theme.name }}</cv-data-table-cell>
                    <cv-data-table-cell>{{ theme.organizationId || 'Global' }}</cv-data-table-cell>
                    <cv-data-table-cell>{{ theme.createdBy }}</cv-data-table-cell>
                    <cv-data-table-cell>
                        <cv-button kind="ghost" size="sm" @click="openEditModal(theme)">
                            Edit
                        </cv-button>
                        <cv-button kind="danger-ghost" size="sm" @click="deleteTheme(theme.id)">
                            <TrashCan20 />
                        </cv-button>
                    </cv-data-table-cell>
                </cv-data-table-row>
            </template>
        </cv-data-table>

        <cv-modal :visible="showModal" @modal-hidden="showModal = false"
            :primary-button-disabled="!themeName || !themeTokens" @primary-click="saveTheme"
            @secondary-click="showModal = false">
            <template v-slot:label>Custom Theme</template>
            <template v-slot:title>{{ editingTheme ? 'Edit' : 'Create' }} Custom Theme</template>
            <template v-slot:content>
                <cv-text-input v-model="themeName" label="Theme Name" placeholder="Enter theme name"
                    class="theme-input" />

                <cv-text-input v-model="organizationId" label="Organization ID (Optional)"
                    placeholder="Leave empty for global theme" class="theme-input" />

                <cv-text-area v-model="themeTokens" label="Theme Tokens (JSON)" placeholder="Enter theme tokens as JSON"
                    rows="15" class="theme-input" />

                <cv-accordion>
                    <cv-accordion-item>
                        <template v-slot:title>Sample Theme JSON</template>
                        <template v-slot:content>
                            <pre class="sample-json">{{ sampleThemeJson }}</pre>
                        </template>
                    </cv-accordion-item>
                </cv-accordion>

                <p class="theme-help">
                    For a complete list of Carbon theme tokens, visit:
                    <a href="https://carbondesignsystem.com/elements/color/tokens/" target="_blank">
                        Carbon Design System Color Tokens
                    </a>
                </p>
            </template>
            <template v-slot:primary-button>Save</template>
            <template v-slot:secondary-button>Cancel</template>
        </cv-modal>
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
