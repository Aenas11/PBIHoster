<template>
    <div class="panel groups-manager">
        <h2>Groups</h2>

        <div v-if="error" class="error-message">{{ error }}</div>

        <div class="controls">
            <cds-text-input :value="term" @input="(e) => (term = (e.target as HTMLInputElement).value)" label="Search"
                placeholder="Search groups" :disabled="loading"></cds-text-input>
            <cds-button kind="primary" @click="search" :disabled="loading">
                {{ loading ? 'Loading...' : 'Search' }}
            </cds-button>
            <cds-button kind="ghost" @click="openCreate" :disabled="loading">Create Group</cds-button>
        </div>

        <cds-table v-if="groups.length > 0" class="groups-table">
            <cds-table-head>
                <cds-table-header-row>
                    <cds-table-header-cell>Name</cds-table-header-cell>
                    <cds-table-header-cell>Description</cds-table-header-cell>
                    <cds-table-header-cell>Actions</cds-table-header-cell>
                </cds-table-header-row>
            </cds-table-head>
            <cds-table-body>
                <cds-table-row v-for="g in groups" :key="g.id">
                    <cds-table-cell>{{ g.name }}</cds-table-cell>
                    <cds-table-cell>{{ g.description || 'No description' }}</cds-table-cell>
                    <cds-table-cell>
                        <cds-button size="sm" kind="ghost" @click="editGroup(g)" :disabled="loading">Edit</cds-button>
                        <cds-button size="sm" kind="danger-ghost" @click="remove(g.id)"
                            :disabled="loading">Delete</cds-button>
                    </cds-table-cell>
                </cds-table-row>
            </cds-table-body>
        </cds-table>

        <div v-else-if="!loading" class="empty-state">No groups found</div>

        <cds-modal :open="showModal" @cds-modal-closed="showModal = false">
            <cds-modal-header>
                <cds-modal-close-button></cds-modal-close-button>
                <cds-modal-label>Group</cds-modal-label>
                <cds-modal-heading>{{ editing ? 'Edit' : 'Create' }} Group</cds-modal-heading>
            </cds-modal-header>
            <cds-modal-body>
                <div v-if="error" class="error-message">{{ error }}</div>
                <cds-text-input label="Name" :value="form.name" @input="onNameInput"
                    :disabled="loading"></cds-text-input>
                <cds-text-input label="Description" :value="form.description" @input="onDescriptionInput"
                    :disabled="loading" placeholder="Optional description"></cds-text-input>
            </cds-modal-body>
            <cds-modal-footer>
                <cds-modal-footer-button kind="secondary" @click="showModal = false"
                    :disabled="loading">Cancel</cds-modal-footer-button>
                <cds-modal-footer-button kind="primary" @click="save" :disabled="loading || !form.name">
                    {{ loading ? 'Saving...' : 'Save' }}
                </cds-modal-footer-button>
            </cds-modal-footer>
        </cds-modal>
    </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { adminService } from '@/services/adminService'
import type { Group } from '@/types/admin'

import '@carbon/web-components/es/components/button/index.js'
import '@carbon/web-components/es/components/text-input/index.js'
import '@carbon/web-components/es/components/modal/index.js'
import '@carbon/web-components/es/components/data-table/index.js'

const term = ref('')
const groups = ref<Group[]>([])
const showModal = ref(false)
const editing = ref(false)
const loading = ref(false)
const error = ref<string | null>(null)

const form = reactive({
    id: undefined as number | undefined,
    name: '',
    description: ''
})

function onNameInput(e: Event) { form.name = (e.target as HTMLInputElement).value }
function onDescriptionInput(e: Event) { form.description = (e.target as HTMLInputElement).value }

async function search() {
    try {
        loading.value = true
        error.value = null
        groups.value = await adminService.searchGroups(term.value)
    } catch (err) {
        error.value = err instanceof Error ? err.message : 'Failed to search groups'
        console.error('Error searching groups:', err)
    } finally {
        loading.value = false
    }
}

function openCreate() {
    editing.value = false
    form.id = undefined
    form.name = ''
    form.description = ''
    error.value = null
    showModal.value = true
}

function editGroup(g: Group) {
    editing.value = true
    form.id = g.id
    form.name = g.name
    form.description = g.description || ''
    error.value = null
    showModal.value = true
}

async function save() {
    try {
        loading.value = true
        error.value = null

        const payload: Group = {
            id: form.id,
            name: form.name,
            description: form.description
        }

        if (editing.value && form.id) {
            await adminService.updateGroup(payload)
        } else {
            await adminService.createGroup(payload)
        }

        showModal.value = false
        await search()
    } catch (err) {
        error.value = err instanceof Error ? err.message : 'Failed to save group'
        console.error('Error saving group:', err)
    } finally {
        loading.value = false
    }
}

async function remove(id?: number) {
    if (!id) return

    const groupToDelete = groups.value.find(g => g.id === id)
    if (!confirm(`Delete group "${groupToDelete?.name}"?`)) return

    try {
        loading.value = true
        error.value = null
        await adminService.deleteGroup(id)
        await search()
    } catch (err) {
        error.value = err instanceof Error ? err.message : 'Failed to delete group'
        console.error('Error deleting group:', err)
    } finally {
        loading.value = false
    }
}

// Initial load
search()
</script>

<style scoped>
.panel {
    padding: 12px;
    border: 1px solid #ddd;
    border-radius: 6px;
    width: 420px;
}

.controls {
    display: flex;
    gap: 8px;
    align-items: end;
    margin-bottom: 8px
}

.groups-table {
    margin-top: 12px
}

.error-message {
    background-color: var(--cds-support-error, #da1e28);
    color: white;
    padding: 8px 12px;
    border-radius: 4px;
    margin-bottom: 12px;
}

.empty-state {
    padding: 24px;
    text-align: center;
    color: var(--cds-text-02, #525252);
}
</style>
