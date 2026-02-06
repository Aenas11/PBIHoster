<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Star20, Document20 } from '@carbon/icons-vue'
import type { DashboardComponentProps } from '../../types/components'
import { useFavoritesStore } from '../../stores/favorites'
import { usePagesStore } from '../../stores/pages'
import { useAuthStore } from '../../stores/auth'
import type { Page } from '../../types/page'

const props = defineProps<DashboardComponentProps>()

const router = useRouter()
const favoritesStore = useFavoritesStore()
const pagesStore = usePagesStore()
const authStore = useAuthStore()

const showFavorites = computed(() => props.config.showFavorites !== false)
const showRecents = computed(() => props.config.showRecents !== false)
const maxItems = computed(() => Number(props.config.maxItems ?? 6))

function flattenPages(tree: Page[], results: Page[] = []) {
    tree.forEach(page => {
        results.push(page)
        if (page.children && page.children.length > 0) {
            flattenPages(page.children, results)
        }
    })
    return results
}

const flatPages = computed(() => flattenPages(pagesStore.pages))

const favoritePages = computed(() =>
    favoritesStore.favoriteIds
        .map(id => flatPages.value.find(page => page.id === id))
        .filter((page): page is Page => !!page)
        .slice(0, maxItems.value)
)

const recentPages = computed(() =>
    favoritesStore.recentIds
        .map(id => flatPages.value.find(page => page.id === id))
        .filter((page): page is Page => !!page)
        .slice(0, maxItems.value)
)

const isEmpty = computed(() =>
    (!showFavorites.value || favoritePages.value.length === 0) &&
    (!showRecents.value || recentPages.value.length === 0)
)

function goToPage(page: Page) {
    router.push({ name: 'page', params: { id: page.id } })
}

onMounted(async () => {
    if (pagesStore.pages.length === 0) {
        await pagesStore.fetchPages()
    }

    if (authStore.isAuthenticated && !favoritesStore.isLoaded) {
        await favoritesStore.loadAll()
    }
})
</script>

<template>
    <div class="favorites-component">
        <div v-if="!authStore.isAuthenticated" class="empty-state">
            Sign in to see your favorites and recent pages.
        </div>

        <template v-else>
            <div class="section" v-if="showFavorites">
                <div class="section-header">
                    <Star20 />
                    <h4>Favorites</h4>
                </div>
                <div v-if="favoritePages.length === 0" class="empty-state">
                    Star pages to see them here.
                </div>
                <ul v-else class="page-list">
                    <li v-for="page in favoritePages" :key="`fav-${page.id}`">
                        <button class="page-link" @click="goToPage(page)">
                            {{ page.title }}
                        </button>
                    </li>
                </ul>
            </div>

            <div class="section" v-if="showRecents">
                <div class="section-header">
                    <Document20 />
                    <h4>Recent</h4>
                </div>
                <div v-if="recentPages.length === 0" class="empty-state">
                    Open pages to build your recent list.
                </div>
                <ul v-else class="page-list">
                    <li v-for="page in recentPages" :key="`recent-${page.id}`">
                        <button class="page-link" @click="goToPage(page)">
                            {{ page.title }}
                        </button>
                    </li>
                </ul>
            </div>

            <div v-if="isEmpty" class="empty-state">
                Favorites and recents will appear here once you start exploring pages.
            </div>
        </template>
    </div>
</template>

<style scoped>
.favorites-component {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
    height: 100%;
}

.section {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.section-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-weight: 600;
}

.section-header h4 {
    margin: 0;
    font-size: 1rem;
}

.page-list {
    list-style: none;
    margin: 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}

.page-link {
    background: none;
    border: none;
    padding: 0.25rem 0;
    text-align: left;
    color: var(--cds-link-primary, #0f62fe);
    cursor: pointer;
    font-size: 0.95rem;
}

.page-link:hover {
    text-decoration: underline;
}

.empty-state {
    color: var(--cds-text-secondary, #525252);
    font-size: 0.9rem;
}
</style>
