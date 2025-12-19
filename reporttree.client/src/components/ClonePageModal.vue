<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { usePagesStore } from '../stores/pages'
import { useToastStore } from '../stores/toast'
import type { Page } from '../types/page'
import '@carbon/web-components/es/components/modal/index.js'
import '@carbon/web-components/es/components/text-input/index.js'
import '@carbon/web-components/es/components/select/index.js'
import '@carbon/web-components/es/components/button/index.js'

const props = defineProps<{
    open: boolean
    page?: Page | null
}>()

const emit = defineEmits(['close'])

const store = usePagesStore()
const toast = useToastStore()

const newTitle = ref('')
const newParentId = ref<number | undefined>(undefined)

// Get all pages flattened for parent selection
const flatPages = computed(() => {
    const flatten = (pages: Page[]): Page[] => {
        const result: Page[] = []
        for (const page of pages) {
            result.push(page)
            if (page.children && page.children.length > 0) {
                result.push(...flatten(page.children))
            }
        }
        return result
    }
    return flatten(store.pages)
})

// Available parents (exclude the page being cloned and its children)
const availableParents = computed(() => {
    if (!props.page) return flatPages.value

    // Recursively get all child IDs
    const getChildIds = (page: Page): number[] => {
        const ids = [page.id]
        if (page.children) {
            for (const child of page.children) {
                ids.push(...getChildIds(child))
            }
        }
        return ids
    }

    const excludedIds = getChildIds(props.page)
    return flatPages.value.filter(p => !excludedIds.includes(p.id))
})

watch(() => props.open, (isOpen) => {
    if (isOpen && props.page) {
        newTitle.value = `${props.page.title} (Copy)`
        newParentId.value = props.page.parentId || undefined
    }
})

async function clone() {
    if (!props.page || !newTitle.value.trim()) {
        toast.warning('Invalid Input', 'Please enter a title for the cloned page')
        return
    }

    try {
        await store.clonePage(
            props.page.id,
            newTitle.value.trim(),
            newParentId.value || null
        )
        toast.success('Page Cloned', `Successfully created "${newTitle.value}"`)
        emit('close')
    } catch (error) {
        console.error('Failed to clone page:', error)
        // Error toast already shown by api client
    }
}
</script>

<template>
    <cds-modal :open="open" @cds-modal-closed="emit('close')">
        <cds-modal-header>
            <cds-modal-close-button></cds-modal-close-button>
            <cds-modal-label>Page Cloning</cds-modal-label>
            <cds-modal-heading>Clone "{{ page?.title }}"</cds-modal-heading>
        </cds-modal-header>
        <cds-modal-body>
            <p style="margin-bottom: 1rem; color: var(--cds-text-secondary);">
                This will create a copy of the page with all its components and configuration.
            </p>

            <cds-text-input label="New Page Title" :value="newTitle" @input="newTitle = $event.target.value"
                placeholder="Enter a title for the cloned page" required></cds-text-input>

            <br />

            <cds-select label-text="Parent Folder (Optional)" :value="newParentId?.toString() || ''"
                @change="newParentId = $event.target.value ? parseInt($event.target.value) : undefined">
                <cds-select-item value="">-- Top Level --</cds-select-item>
                <cds-select-item v-for="parent in availableParents" :key="parent.id" :value="parent.id.toString()">
                    {{ parent.title }}
                </cds-select-item>
            </cds-select>
        </cds-modal-body>
        <cds-modal-footer>
            <cds-modal-footer-button kind="secondary" @click="emit('close')">Cancel</cds-modal-footer-button>
            <cds-modal-footer-button kind="primary" @click="clone">Clone Page</cds-modal-footer-button>
        </cds-modal-footer>
    </cds-modal>
</template>
