import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { api } from '../services/api'

export const useFavoritesStore = defineStore('favorites', () => {
    const favoriteIds = ref<number[]>([])
    const recentIds = ref<number[]>([])
    const isLoaded = ref(false)

    const favoriteSet = computed(() => new Set(favoriteIds.value))

    function isFavorite(pageId: number) {
        return favoriteSet.value.has(pageId)
    }

    async function loadFavorites() {
        favoriteIds.value = await api.get<number[]>('/profile/favorites')
    }

    async function loadRecents() {
        recentIds.value = await api.get<number[]>('/profile/recent')
    }

    async function toggleFavorite(pageId: number) {
        if (isFavorite(pageId)) {
            await api.delete(`/profile/favorites/${pageId}`)
            favoriteIds.value = favoriteIds.value.filter(id => id !== pageId)
            return
        }

        await api.post(`/profile/favorites/${pageId}`)
        favoriteIds.value = [pageId, ...favoriteIds.value.filter(id => id !== pageId)]
    }

    async function recordRecent(pageId: number) {
        await api.post(`/profile/recent/${pageId}`)
        recentIds.value = [pageId, ...recentIds.value.filter(id => id !== pageId)].slice(0, 10)
    }

    async function loadAll() {
        await Promise.all([loadFavorites(), loadRecents()])
        isLoaded.value = true
    }

    return {
        favoriteIds,
        recentIds,
        isLoaded,
        isFavorite,
        loadFavorites,
        loadRecents,
        toggleFavorite,
        recordRecent,
        loadAll
    }
})
