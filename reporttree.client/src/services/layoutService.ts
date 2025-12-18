import type { GridItemWithComponent } from '../types/layout'
import { useAuthStore } from '../stores/auth'

export interface SaveLayoutRequest {
    pageId: string
    layout: GridItemWithComponent[]
    metadata?: {
        name?: string
        description?: string
        createdAt?: string
    }
}

export interface SaveLayoutResponse {
    success: boolean
    layoutId: string
    message: string
}

interface SavedLayout extends SaveLayoutRequest {
    id: string
    savedAt: string
}

/**
 * API service for saving and loading page layouts
 */
export const layoutService = {
    /**
     * Save the current grid layout to the server
     * @param request Layout data to save
     * @returns Promise with save result
     */
    async saveLayout(request: SaveLayoutRequest): Promise<SaveLayoutResponse> {
        const auth = useAuthStore()
        try {
            const res = await fetch(`/api/pages/${request.pageId}/layout`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${auth.token}`
                },
                body: JSON.stringify(request.layout)
            })

            if (!res.ok) throw new Error('Failed to save layout')

            return {
                success: true,
                layoutId: 'current',
                message: 'Layout saved successfully'
            }
        } catch (e) {
            console.error(e)
            return { success: false, layoutId: '', message: 'Error saving layout' }
        }
    },


    /**
     * Get all saved layouts for a page
     * @param pageId The page ID to get layouts for
     * @returns Promise with array of layouts
     */
    async getLayoutsByPage(pageId: string): Promise<SaveLayoutRequest[]> {
        const auth = useAuthStore()
        try {
            const headers: HeadersInit = {}
            if (auth.token) {
                headers['Authorization'] = `Bearer ${auth.token}`
            }

            const res = await fetch(`/api/pages/${pageId}`, { headers })
            if (!res.ok) throw new Error('Failed to fetch page')

            const page = await res.json()
            let layout: GridItemWithComponent[] = []
            if (page.layout) {
                try {
                    layout = JSON.parse(page.layout)
                } catch {
                    console.warn('Failed to parse layout JSON')
                }
            }

            // Return in the format expected by useGridLayout (array of "saved layouts")
            // We wrap the current layout in an object that matches SaveLayoutRequest structure
            return [{
                pageId,
                layout,
                metadata: { createdAt: new Date().toISOString() }
            }]
        } catch (e) {
            console.error(e)
            return []
        }
    }
}
