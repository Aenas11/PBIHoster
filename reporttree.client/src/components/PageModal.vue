<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { usePagesStore } from '../stores/pages'
import { useAuthStore } from '../stores/auth'
import type { Page } from '../types/page'
import { Close20 } from '@carbon/icons-vue'
import ClonePageModal from './ClonePageModal.vue'
import '@carbon/web-components/es/components/modal/index.js';
import '@carbon/web-components/es/components/text-input/index.js';
import '@carbon/web-components/es/components/select/index.js';
import '@carbon/web-components/es/components/button/index.js';
import '@carbon/web-components/es/components/tabs/index.js';
import '@carbon/web-components/es/components/checkbox/index.js';
import '@carbon/web-components/es/components/tag/index.js';

const props = defineProps<{
    open: boolean
    page?: Page | null
    parentId?: number | null
}>()

const emit = defineEmits(['close', 'create-child'])

const store = usePagesStore()
const auth = useAuthStore()

const isCloneModalOpen = ref(false)

const formData = ref({
    title: '',
    icon: 'Document20',
    parentId: undefined as number | undefined,
    isPublic: false,
    allowedUsers: [] as string[],
    allowedGroups: [] as string[]
})

const isEdit = computed(() => !!props.page)

watch(() => props.open, (isOpen) => {
    if (isOpen) {
        if (props.page) {
            formData.value = {
                title: props.page.title,
                icon: props.page.icon,
                parentId: props.page.parentId,
                isPublic: props.page.isPublic,
                allowedUsers: props.page.allowedUsers || [],
                allowedGroups: props.page.allowedGroups || []
            }
        } else {
            formData.value = {
                title: '',
                icon: 'Document20',
                parentId: props.parentId || undefined,
                isPublic: false,
                allowedUsers: [],
                allowedGroups: []
            }
        }
    }
})

const icons = ['Dashboard20', 'Document20', 'Folder20', 'ChartBar20', 'Table20']

const searchQuery = ref('')
const searchResults = ref<{ type: 'user' | 'group', name: string }[]>([])

let searchTimeout: ReturnType<typeof setTimeout>

function handleSearchInput(event: any) {
    searchQuery.value = event.target.value
    clearTimeout(searchTimeout)
    searchTimeout = setTimeout(search, 300)
}

async function search() {
    if (!searchQuery.value) {
        searchResults.value = []
        return
    }

    try {
        const headers: HeadersInit = {}
        if (auth.token) {
            headers['Authorization'] = `Bearer ${auth.token}`
        }

        const [usersRes, groupsRes] = await Promise.all([
            fetch(`/api/directory/users?query=${searchQuery.value}`, { headers }),
            fetch(`/api/directory/groups?query=${searchQuery.value}`, { headers })
        ])

        const users = await usersRes.json()
        const groups = await groupsRes.json()

        searchResults.value = [
            ...users.map((u: any) => ({ type: 'user', name: u.username })),
            ...groups.map((g: any) => ({ type: 'group', name: g.name }))
        ]
    } catch (e) {
        console.error(e)
    }
}

function addEntity(entity: { type: 'user' | 'group', name: string }) {
    if (entity.type === 'user') {
        if (!formData.value.allowedUsers.includes(entity.name)) {
            formData.value.allowedUsers.push(entity.name)
        }
    } else {
        if (!formData.value.allowedGroups.includes(entity.name)) {
            formData.value.allowedGroups.push(entity.name)
        }
    }
    searchQuery.value = ''
    searchResults.value = []
}

function removeUser(username: string) {
    formData.value.allowedUsers = formData.value.allowedUsers.filter(u => u !== username)
}

function removeGroup(groupname: string) {
    formData.value.allowedGroups = formData.value.allowedGroups.filter(g => g !== groupname)
}

async function save() {
    if (isEdit.value && props.page) {
        await store.updatePage({
            ...props.page,
            ...formData.value,
            order: props.page.order,
        })
    } else {
        await store.createPage({
            ...formData.value,
            order: 0, // Default order            
        })
    }
    emit('close')
}

async function remove() {
    if (props.page) {
        if (confirm('Are you sure you want to delete this page?')) {
            await store.deletePage(props.page.id)
            emit('close')
        }
    }
}

function createChild() {
    if (props.page) {
        emit('create-child', props.page.id)
    }
}

function openCloneModal() {
    isCloneModalOpen.value = true
}

function closeCloneModal() {
    isCloneModalOpen.value = false
    emit('close') // Also close the parent modal after cloning
}
</script>

<template>
    <div>
        <cds-modal :open="open" @cds-modal-closed="emit('close')">
            <cds-modal-header>
                <cds-modal-close-button></cds-modal-close-button>
                <cds-modal-label>Page Management</cds-modal-label>
                <cds-modal-heading>{{ isEdit ? 'Edit Page' : 'Create Page' }}</cds-modal-heading>
            </cds-modal-header>
            <cds-modal-body>
                <cds-tabs value="general">
                    <cds-tab target="panel-general" value="general">General</cds-tab>
                    <cds-tab target="panel-permissions" value="permissions">Permissions</cds-tab>
                </cds-tabs>

                <div id="panel-general" role="tabpanel" aria-labelledby="tab-general">
                    <br />
                    <cds-text-input label="Title" :value="formData.title" @input="formData.title = $event.target.value"
                        placeholder="Page Title"></cds-text-input>
                    <br />
                    <cds-select label-text="Icon" :value="formData.icon" @change="formData.icon = $event.target.value">
                        <cds-select-item v-for="icon in icons" :key="icon" :value="icon">{{ icon }}</cds-select-item>
                    </cds-select>
                </div>

                <div id="panel-permissions" role="tabpanel" aria-labelledby="tab-permissions" hidden>
                    <br />
                    <cds-checkbox :checked="formData.isPublic"
                        @cds-checkbox-changed="formData.isPublic = $event.detail.checked">Public Page</cds-checkbox>

                    <div v-if="!formData.isPublic" style="margin-top: 1rem;">
                        <cds-text-input label="Search Users or Groups" :value="searchQuery" @input="handleSearchInput"
                            placeholder="Type to search..."></cds-text-input>

                        <div v-if="searchResults.length > 0" class="search-results">
                            <div v-for="result in searchResults" :key="result.type + result.name"
                                class="search-result-item" @click="addEntity(result)">
                                <strong>{{ result.type === 'user' ? 'User' : 'Group' }}:</strong> {{ result.name }}
                            </div>
                        </div>

                        <div style="margin-top: 1rem;">
                            <p style="margin-bottom: 0.5rem; font-size: 0.875rem; font-weight: 600;">Allowed Users</p>
                            <div v-if="formData.allowedUsers.length === 0"
                                style="font-style: italic; color: var(--cds-text-secondary);">No users added</div>
                            <div class="tags-container">
                                <cds-tag v-for="user in formData.allowedUsers" :key="user" type="blue"
                                    title="Remove User">
                                    {{ user }}
                                    <button class="cds--tag__close-icon" @click="removeUser(user)"
                                        aria-label="Remove User">
                                        <Close20 />
                                    </button>
                                </cds-tag>
                            </div>
                        </div>

                        <div style="margin-top: 1rem;">
                            <p style="margin-bottom: 0.5rem; font-size: 0.875rem; font-weight: 600;">Allowed Groups</p>
                            <div v-if="formData.allowedGroups.length === 0"
                                style="font-style: italic; color: var(--cds-text-secondary);">No groups added</div>
                            <div class="tags-container">
                                <cds-tag v-for="group in formData.allowedGroups" :key="group" type="purple"
                                    title="Remove Group">
                                    {{ group }}
                                    <button class="cds--tag__close-icon" @click="removeGroup(group)"
                                        aria-label="Remove Group">
                                        <Close20 />
                                    </button>
                                </cds-tag>
                            </div>
                        </div>
                    </div>
                </div>
            </cds-modal-body>
            <cds-modal-footer>
                <cds-modal-footer-button kind="secondary" @click="emit('close')">Cancel</cds-modal-footer-button>
                <cds-modal-footer-button v-if="isEdit" kind="tertiary" @click="openCloneModal">Clone
                    Page</cds-modal-footer-button>
                <cds-modal-footer-button v-if="isEdit" kind="ghost" @click="createChild">Add Child
                    Page</cds-modal-footer-button>
                <cds-modal-footer-button v-if="isEdit" kind="danger" @click="remove">Delete</cds-modal-footer-button>
                <cds-modal-footer-button kind="primary" @click="save">Save</cds-modal-footer-button>
            </cds-modal-footer>
        </cds-modal>

        <!-- Clone Page Modal -->
        <ClonePageModal :open="isCloneModalOpen" :page="page" @close="closeCloneModal" />
    </div>
</template>

<style scoped>
.search-results {
    border: 1px solid var(--cds-border-subtle);
    max-height: 150px;
    overflow-y: auto;
    margin-top: 0.25rem;
    background-color: var(--cds-layer);
    position: absolute;
    width: calc(100% - 2rem);
    z-index: 1000;
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

.search-result-item {
    padding: 0.5rem 1rem;
    cursor: pointer;
}

.search-result-item:hover {
    background-color: var(--cds-layer-hover);
}

.tags-container {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
}
</style>
