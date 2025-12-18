<template>
    <div class="panel users-manager">
        <h2>Users</h2>

        <div v-if="error" class="error-message">{{ error }}</div>

        <div class="controls">
            <cds-text-input :value="term" @input="(e) => (term = (e.target as HTMLInputElement).value)" label="Search"
                placeholder="Search users" :disabled="loading"></cds-text-input>
            <cds-button kind="primary" @click="search" :disabled="loading">
                {{ loading ? 'Loading...' : 'Search' }}
            </cds-button>
            <cds-button kind="ghost" @click="openCreate" :disabled="loading">Create User</cds-button>
        </div>

        <cds-table v-if="users.length > 0" class="users-table">
            <cds-table-head>
                <cds-table-header-row>
                    <cds-table-header-cell>Username</cds-table-header-cell>
                    <cds-table-header-cell>Roles</cds-table-header-cell>
                    <cds-table-header-cell>Groups</cds-table-header-cell>
                    <cds-table-header-cell>Actions</cds-table-header-cell>
                </cds-table-header-row>
            </cds-table-head>
            <cds-table-body>
                <cds-table-row v-for="u in users" :key="u.id">
                    <cds-table-cell>{{ u.username }}</cds-table-cell>
                    <cds-table-cell>{{ (u.roles || []).join(', ') || 'None' }}</cds-table-cell>
                    <cds-table-cell>{{ (u.groups || []).join(', ') || 'None' }}</cds-table-cell>
                    <cds-table-cell>
                        <cds-button size="sm" kind="ghost" @click="editUser(u)" :disabled="loading">Edit</cds-button>
                        <cds-button size="sm" kind="danger-ghost" @click="deleteUser(u.username)"
                            :disabled="loading">Delete</cds-button>
                    </cds-table-cell>
                </cds-table-row>
            </cds-table-body>
        </cds-table>

        <div v-else-if="!loading" class="empty-state">No users found</div>

        <cds-modal :open="showModal" @cds-modal-closed="showModal = false">
            <cds-modal-header>
                <cds-modal-close-button></cds-modal-close-button>
                <cds-modal-label>User</cds-modal-label>
                <cds-modal-heading>{{ editing ? 'Edit' : 'Create' }} User</cds-modal-heading>
            </cds-modal-header>
            <cds-modal-body>
                <div v-if="error" class="error-message">{{ error }}</div>
                <cds-text-input label="Username" :value="form.username" @input="onUsernameInput"
                    :disabled="loading || editing"></cds-text-input>
                <cds-text-input label="Password" type="password" :value="form.password" @input="onPasswordInput"
                    :disabled="loading"
                    :helper-text="editing ? 'Leave blank to keep current password' : ''"></cds-text-input>
                <cds-text-input label="Roles (comma separated)" :value="form.rolesText" @input="onRolesInput"
                    :disabled="loading" placeholder="e.g., Admin, Editor, Viewer"></cds-text-input>
                <cds-text-input label="Groups (comma separated)" :value="form.groupsText" @input="onGroupsInput"
                    :disabled="loading" placeholder="e.g., Finance, IT, Sales"></cds-text-input>
            </cds-modal-body>
            <cds-modal-footer>
                <cds-modal-footer-button kind="secondary" @click="showModal = false"
                    :disabled="loading">Cancel</cds-modal-footer-button>
                <cds-modal-footer-button kind="primary" @click="save" :disabled="loading || !form.username">
                    {{ loading ? 'Saving...' : 'Save' }}
                </cds-modal-footer-button>
            </cds-modal-footer>
        </cds-modal>
    </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { adminService } from '@/services/adminService'
import type { AppUser } from '@/types/admin'

import '@carbon/web-components/es/components/button/index.js'
import '@carbon/web-components/es/components/text-input/index.js'
import '@carbon/web-components/es/components/modal/index.js'
import '@carbon/web-components/es/components/data-table/index.js'

const term = ref('')
const users = ref<AppUser[]>([])
const showModal = ref(false)
const editing = ref(false)
const loading = ref(false)
const error = ref<string | null>(null)

const form = reactive({
    username: '',
    password: '',
    rolesText: '',
    groupsText: ''
})

function onUsernameInput(e: Event) { form.username = (e.target as HTMLInputElement).value }
function onPasswordInput(e: Event) { form.password = (e.target as HTMLInputElement).value }
function onRolesInput(e: Event) { form.rolesText = (e.target as HTMLInputElement).value }
function onGroupsInput(e: Event) { form.groupsText = (e.target as HTMLInputElement).value }

async function search() {
    try {
        loading.value = true
        error.value = null
        users.value = await adminService.searchUsers(term.value)
    } catch (err) {
        error.value = err instanceof Error ? err.message : 'Failed to search users'
        console.error('Error searching users:', err)
    } finally {
        loading.value = false
    }
}

function openCreate() {
    editing.value = false
    form.username = ''
    form.password = ''
    form.rolesText = ''
    form.groupsText = ''
    error.value = null
    showModal.value = true
}

function editUser(u: AppUser) {
    editing.value = true
    form.username = u.username
    form.password = ''
    form.rolesText = (u.roles || []).join(', ')
    form.groupsText = (u.groups || []).join(', ')
    error.value = null
    showModal.value = true
}

async function save() {
    try {
        loading.value = true
        error.value = null

        const payload = {
            username: form.username,
            roles: form.rolesText ? form.rolesText.split(',').map((s: string) => s.trim()).filter(Boolean) : [],
            groups: form.groupsText ? form.groupsText.split(',').map((s: string) => s.trim()).filter(Boolean) : [],
            password: form.password || undefined
        }

        await adminService.upsertUser(payload)
        await search()
        showModal.value = false
    } catch (err) {
        error.value = err instanceof Error ? err.message : 'Failed to save user'
        console.error('Error saving user:', err)
    } finally {
        loading.value = false
    }
}

async function deleteUser(username?: string) {
    if (!username) return
    if (!confirm(`Delete user "${username}"?`)) return

    try {
        loading.value = true
        error.value = null
        await adminService.deleteUser(username)
        await search()
    } catch (err) {
        error.value = err instanceof Error ? err.message : 'Failed to delete user'
        console.error('Error deleting user:', err)
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
    width: 540px;
}

.controls {
    display: flex;
    gap: 8px;
    align-items: end;
    margin-bottom: 8px
}

.users-table {
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
