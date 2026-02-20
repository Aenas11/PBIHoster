<template>
    <div class="panel rls-manager">
        <div class="header">
            <div>
                <h2>RLS Configuration</h2>
                <p class="subtitle">Row-Level Security settings across all pages and components.</p>
            </div>
            <cds-button kind="ghost" size="sm" @click="load">Refresh</cds-button>
        </div>

        <div v-if="loading" class="empty-state">Loading...</div>

        <cds-table v-else-if="rlsItems.length > 0" class="rls-table">
            <cds-table-head>
                <cds-table-header-row>
                    <cds-table-header-cell>Page</cds-table-header-cell>
                    <cds-table-header-cell>Component</cds-table-header-cell>
                    <cds-table-header-cell>Type</cds-table-header-cell>
                    <cds-table-header-cell>RLS Roles</cds-table-header-cell>
                    <cds-table-header-cell>Actions</cds-table-header-cell>
                </cds-table-header-row>
            </cds-table-head>
            <cds-table-body>
                <cds-table-row v-for="item in rlsItems" :key="`${item.pageId}-${item.componentId}`">
                    <cds-table-cell>
                        <router-link :to="`/page/${item.pageId}`" class="link">{{ item.pageTitle }}</router-link>
                    </cds-table-cell>
                    <cds-table-cell>
                        <span class="muted">{{ item.componentTitle || item.componentId }}</span>
                    </cds-table-cell>
                    <cds-table-cell>{{ item.componentType }}</cds-table-cell>
                    <cds-table-cell>
                        <div class="roles">
                            <cds-tag v-for="role in item.rlsRoles" :key="role" type="blue" size="sm">
                                {{ role }}
                            </cds-tag>
                            <span v-if="item.rlsRoles.length === 0" class="muted">No roles (user identity only)</span>
                        </div>
                    </cds-table-cell>
                    <cds-table-cell>
                        <router-link :to="`/page/${item.pageId}`" class="link">Edit page</router-link>
                    </cds-table-cell>
                </cds-table-row>
            </cds-table-body>
        </cds-table>

        <div v-else class="empty-state">No components with RLS enabled found.</div>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import type { Page } from '@/types/page'
import { api } from '@/services/api'

import '@carbon/web-components/es/components/button/index.js'
import '@carbon/web-components/es/components/data-table/index.js'
import '@carbon/web-components/es/components/tag/index.js'

interface RLSItem {
    pageId: number
    pageTitle: string
    componentId: string
    componentTitle: string
    componentType: string
    rlsRoles: string[]
}

const loading = ref(false)
const rlsItems = ref<RLSItem[]>([])

async function load() {
    loading.value = true
    rlsItems.value = []
    try {
        const pages = await api.get<Page[]>('/pages')
        const items: RLSItem[] = []

        for (const page of pages) {
            if (!page.layout) continue
            try {
                const layout = JSON.parse(page.layout)
                if (!Array.isArray(layout)) continue
                for (const component of layout) {
                    const config = component.componentConfig
                    if (!config || !config.enableRLS) continue
                    const roles: string[] = Array.isArray(config.rlsRoles) ? config.rlsRoles : []
                    items.push({
                        pageId: page.id,
                        pageTitle: page.title,
                        componentId: component.i ?? '',
                        componentTitle: component.metadata?.title ?? '',
                        componentType: component.componentType ?? '',
                        rlsRoles: roles
                    })
                }
            } catch {
                // Skip pages with invalid layout JSON
            }
        }

        rlsItems.value = items
    } catch (e) {
        console.error('Failed to load RLS configuration', e)
    } finally {
        loading.value = false
    }
}

onMounted(load)
</script>

<style scoped>
.panel {
    padding: 12px;
    border: 1px solid #ddd;
    border-radius: 6px;
    width: 100%;
}

.header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    margin-bottom: 1rem;
}

.subtitle {
    margin: 0.25rem 0 0 0;
    color: var(--cds-text-secondary);
    font-size: 0.9rem;
}

.rls-table {
    margin-top: 0.5rem;
}

.roles {
    display: flex;
    flex-wrap: wrap;
    gap: 0.25rem;
}

.muted {
    color: var(--cds-text-02, #525252);
    font-size: 0.85rem;
}

.link {
    color: var(--cds-link-primary, #0f62fe);
    text-decoration: none;
}

.link:hover {
    text-decoration: underline;
}

.empty-state {
    padding: 1rem 0;
    color: var(--cds-text-02, #525252);
}
</style>
