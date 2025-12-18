import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { Page } from '../types/page'
import { useAuthStore } from './auth'

export const usePagesStore = defineStore('pages', () => {
    const pages = ref<Page[]>([])
    const isLoading = ref(false)
    const auth = useAuthStore()

    const fetchPages = async () => {
        isLoading.value = true
        try {
            const headers: HeadersInit = {}
            if (auth.token) {
                headers['Authorization'] = `Bearer ${auth.token}`
            }
            const res = await fetch('/api/pages', {
                headers
            })
            if (res.ok) {
                const allPages = await res.json() as Page[]
                pages.value = buildTree(allPages)
            }
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
        const res = await fetch('/api/pages', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${auth.token}`
            },
            body: JSON.stringify(page)
        })
        if (res.ok) {
            await fetchPages()
        }
    }

    const updatePage = async (page: Page) => {
        const res = await fetch(`/api/pages/${page.id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${auth.token}`
            },
            body: JSON.stringify(page)
        })
        if (res.ok) {
            await fetchPages()
        }
    }

    const deletePage = async (id: number) => {
        const res = await fetch(`/api/pages/${id}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${auth.token}`
            }
        })
        if (res.ok) {
            await fetchPages()
        }
    }

    return {
        pages,
        isLoading,
        fetchPages,
        createPage,
        updatePage,
        deletePage
    }
})
