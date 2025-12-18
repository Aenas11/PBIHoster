<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { usePagesStore } from '../stores/pages'
import type { Page } from '../types/page'
import '@carbon/web-components/es/components/modal/index.js';
import '@carbon/web-components/es/components/text-input/index.js';
import '@carbon/web-components/es/components/select/index.js';
import '@carbon/web-components/es/components/button/index.js';

const props = defineProps<{
    open: boolean
    page?: Page | null
    parentId?: number | null
}>()

const emit = defineEmits(['close', 'create-child'])

const store = usePagesStore()

const formData = ref({
    title: '',
    icon: 'Document20',
    parentId: undefined as number | undefined,
    roles: [] as string[]
})

const isEdit = computed(() => !!props.page)

watch(() => props.open, (isOpen) => {
    if (isOpen) {
        if (props.page) {
            formData.value = {
                title: props.page.title,
                icon: props.page.icon,
                parentId: props.page.parentId,
                roles: props.page.roles
            }
        } else {
            formData.value = {
                title: '',
                icon: 'Document20',
                parentId: props.parentId || undefined,
                roles: []
            }
        }
    }
})

const icons = ['Dashboard20', 'Document20', 'Folder20', 'ChartBar20', 'Table20']

async function save() {
    if (isEdit.value && props.page) {
        await store.updatePage({
            ...props.page,
            ...formData.value,
            order: props.page.order
        })
    } else {
        await store.createPage({
            ...formData.value,
            order: 0, // Default order
            roles: [] // Default roles
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
</script>

<template>
    <cds-modal :open="open" @cds-modal-closed="emit('close')">
        <cds-modal-header>
            <cds-modal-close-button></cds-modal-close-button>
            <cds-modal-label>Page Management</cds-modal-label>
            <cds-modal-heading>{{ isEdit ? 'Edit Page' : 'Create Page' }}</cds-modal-heading>
        </cds-modal-header>
        <cds-modal-body>
            <cds-text-input label="Title" :value="formData.title" @input="formData.title = $event.target.value"
                placeholder="Page Title"></cds-text-input>
            <br />
            <cds-select label-text="Icon" :value="formData.icon" @change="formData.icon = $event.target.value">
                <cds-select-item v-for="icon in icons" :key="icon" :value="icon">{{ icon }}</cds-select-item>
            </cds-select>
        </cds-modal-body>
        <cds-modal-footer>
            <cds-modal-footer-button kind="secondary" @click="emit('close')">Cancel</cds-modal-footer-button>
            <cds-modal-footer-button v-if="isEdit" kind="ghost" @click="createChild">Add Child
                Page</cds-modal-footer-button>
            <cds-modal-footer-button v-if="isEdit" kind="danger" @click="remove">Delete</cds-modal-footer-button>
            <cds-modal-footer-button kind="primary" @click="save">Save</cds-modal-footer-button>
        </cds-modal-footer>
    </cds-modal>
</template>
