import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { Page } from '../types/page'
import { api } from '../services/api'

export const usePagesStore = defineStore('pages', () => {
    const pages = ref<Page[]>([])
    const isLoading = ref(false)

    const fetchPages = async () => {
        isLoading.value = true
        try {
            const allPages = await api.get<Page[]>('/pages')
            pages.value = buildTree(allPages)
        } catch (e) {
            console.error(e)
        } finally {
            isLoading.value = false
        }
    }

    const buildTree = (flatPages: Page[]): Page[] => {
        const root: Page[] = []
        const map = new Map<number, Page>()

        // First pass: create map and initialize children
        flatPages.forEach(p => {
            p.children = []
            map.set(p.id, p)
        })

        // Second pass: link children
        flatPages.forEach(p => {
            if (p.parentId && map.has(p.parentId)) {
                map.get(p.parentId)!.children!.push(p)
            } else {
                root.push(p)
            }
        })

        return root
    }

    const createPage = async (page: Omit<Page, 'id'>) => {
        await api.post('/pages', page)
        await fetchPages()
    }

    const updatePage = async (page: Page) => {
        await api.put(`/pages/${page.id}`, page)
        await fetchPages()
    }

    const deletePage = async (id: number) => {
        await api.delete(`/pages/${id}`)
        await fetchPages()
    }

    const clonePage = async (id: number, newTitle?: string, newParentId?: number | null) => {
        const response = await api.post<Page>(`/pages/${id}/clone`, {
            newTitle,
            newParentId
        })
        await fetchPages()
        return response
    }

    return {
        pages,
        isLoading,
        fetchPages,
        createPage,
        updatePage,
        deletePage,
        clonePage
    }
})
