<template>
    <div class="panel groups-manager">
        <h2>Groups</h2>

        <div v-if="error" class="error-message">{{ error }}</div>

        <div class="controls">
            <cds-text-input :value="term" @input="(e: Event) => (term = (e.target as HTMLInputElement).value)"
                label="Search" placeholder="Search groups" :disabled="loading"></cds-text-input>
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
                <cds-text-input label="Name" :value="form.name" @input="onNameInput"
                    :disabled="loading"></cds-text-input>
                <cds-text-input label="Description" :value="form.description" @input="onDescriptionInput"
                    :disabled="loading" placeholder="Optional description"></cds-text-input>

                <div v-if="editing && form.id" class="members-section">
                    <h3>Members</h3>
                    <div class="add-member-controls">
                        <div class="user-search-container">
                            <cds-text-input label="Add Member" :value="userSearchTerm" @input="onUserSearchInput"
                                @focus="showUserSuggestions = true" :disabled="loading" placeholder="Search users...">
                            </cds-text-input>
                            <div v-if="showUserSuggestions && filteredUsers.length > 0" class="user-suggestions">
                                <div v-for="user in filteredUsers" :key="user.username" class="user-suggestion-item"
                                    @click="selectUser(user.username)">
                                    <span class="username">{{ user.username }}</span>
                                    <span v-if="user.roles && user.roles.length > 0" class="user-roles">{{
                                        user.roles.join(', ') }}</span>
                                </div>
                            </div>
                            <div v-else-if="showUserSuggestions && userSearchTerm && filteredUsers.length === 0"
                                class="no-suggestions">
                                No users found
                            </div>
                        </div>
                        <cds-button size="sm" kind="primary" @click="addMember"
                            :disabled="loading || !selectedUsername">
                            Add
                        </cds-button>
                    </div>
                    <div v-if="form.members && form.members.length > 0" class="members-list">
                        <div v-for="member in form.members" :key="member" class="member-item">
                            <span>{{ member }}</span>
                            <cds-button size="sm" kind="danger-ghost" @click="removeMember(member)" :disabled="loading">
                                Remove
                            </cds-button>
                        </div>
                    </div>
                    <div v-else class="empty-members">No members yet</div>
                </div>
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
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue'
import { adminService } from '@/services/adminService'
import type { Group, AppUser } from '@/types/admin'

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

// User search state
const allUsers = ref<AppUser[]>([])
const userSearchTerm = ref('')
const selectedUsername = ref('')
const showUserSuggestions = ref(false)

const form = reactive({
    id: undefined as number | undefined,
    name: '',
    description: '',
    members: [] as string[]
})

// Computed filtered users based on search term and excluding current members
const filteredUsers = computed(() => {
    if (!userSearchTerm.value) return []

    const searchLower = userSearchTerm.value.toLowerCase()
    return allUsers.value
        .filter(u => !form.members.includes(u.username)) // Exclude already added members
        .filter(u => u.username.toLowerCase().includes(searchLower))
        .slice(0, 10) // Limit to 10 suggestions
})

function onNameInput(e: Event) { form.name = (e.target as HTMLInputElement).value }
function onDescriptionInput(e: Event) { form.description = (e.target as HTMLInputElement).value }

function onUserSearchInput(e: Event) {
    userSearchTerm.value = (e.target as HTMLInputElement).value
    selectedUsername.value = '' // Clear selection when typing
    showUserSuggestions.value = true
}

function selectUser(username: string) {
    selectedUsername.value = username
    userSearchTerm.value = username
    showUserSuggestions.value = false
}

// Close suggestions when clicking outside
function handleClickOutside(e: MouseEvent) {
    const target = e.target as HTMLElement
    if (!target.closest('.user-search-container')) {
        showUserSuggestions.value = false
    }
}

onMounted(() => {
    document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
    document.removeEventListener('click', handleClickOutside)
})

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
    form.members = []
    userSearchTerm.value = ''
    selectedUsername.value = ''
    showUserSuggestions.value = false
    error.value = null
    showModal.value = true
}

async function editGroup(g: Group) {
    editing.value = true
    form.id = g.id
    form.name = g.name
    form.description = g.description || ''
    form.members = g.members ? [...g.members] : []
    userSearchTerm.value = ''
    selectedUsername.value = ''
    showUserSuggestions.value = false
    error.value = null
    showModal.value = true

    // Load all users for search
    try {
        allUsers.value = await adminService.searchUsers('')
    } catch (err) {
        console.error('Error loading users:', err)
    }
}

async function save() {
    try {
        loading.value = true
        error.value = null

        const payload: Group = {
            id: form.id,
            name: form.name,
            description: form.description,
            members: form.members // Include members to preserve them
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

async function addMember() {
    if (!form.id || !selectedUsername.value.trim()) return

    try {
        loading.value = true
        error.value = null
        await adminService.addGroupMember(form.id, selectedUsername.value.trim())

        // Update local state
        if (!form.members.includes(selectedUsername.value.trim())) {
            form.members.push(selectedUsername.value.trim())
        }

        // Reset search
        userSearchTerm.value = ''
        selectedUsername.value = ''
        showUserSuggestions.value = false

        // Refresh the group list
        await search()
    } catch (err) {
        error.value = err instanceof Error ? err.message : 'Failed to add member'
        console.error('Error adding member:', err)
    } finally {
        loading.value = false
    }
}

async function removeMember(username: string) {
    if (!form.id) return
    if (!confirm(`Remove "${username}" from group?`)) return

    try {
        loading.value = true
        error.value = null
        await adminService.removeGroupMember(form.id, username)

        // Update local state
        form.members = form.members.filter(m => m !== username)

        // Refresh the group list
        await search()
    } catch (err) {
        error.value = err instanceof Error ? err.message : 'Failed to remove member'
        console.error('Error removing member:', err)
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

.members-section {
    margin-top: 24px;
    padding-top: 16px;
    border-top: 1px solid var(--cds-border-subtle, #e0e0e0);
}

.members-section h3 {
    margin: 0 0 12px 0;
    font-size: 14px;
    font-weight: 600;
}

.add-member-controls {
    display: flex;
    gap: 8px;
    align-items: end;
    margin-bottom: 16px;
}

.user-search-container {
    position: relative;
    flex: 1;
}

.user-suggestions {
    position: absolute;
    top: 100%;
    left: 0;
    right: 0;
    z-index: 1000;
    background: white;
    border: 1px solid var(--cds-border-subtle, #e0e0e0);
    border-top: none;
    border-radius: 0 0 4px 4px;
    max-height: 300px;
    overflow-y: auto;
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

.user-suggestion-item {
    padding: 10px 12px;
    cursor: pointer;
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-bottom: 1px solid var(--cds-border-subtle, #f4f4f4);
    transition: background-color 0.15s;
}

.user-suggestion-item:hover {
    background-color: var(--cds-layer-hover, #e8e8e8);
}

.user-suggestion-item:last-child {
    border-bottom: none;
}

.user-suggestion-item .username {
    font-weight: 500;
    color: var(--cds-text-01, #161616);
}

.user-suggestion-item .user-roles {
    font-size: 12px;
    color: var(--cds-text-02, #525252);
    margin-left: 8px;
}

.no-suggestions {
    padding: 12px;
    text-align: center;
    color: var(--cds-text-02, #525252);
    font-style: italic;
    background: white;
    border: 1px solid var(--cds-border-subtle, #e0e0e0);
    border-top: none;
    border-radius: 0 0 4px 4px;
}

.members-list {
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.member-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 8px 12px;
    background-color: var(--cds-layer-01, #f4f4f4);
    border-radius: 4px;
}

.empty-members {
    padding: 16px;
    text-align: center;
    color: var(--cds-text-02, #525252);
    font-style: italic;
}
</style>
