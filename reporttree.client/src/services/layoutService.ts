import type { GridItem } from '../composables/useGridLayout'

export interface SaveLayoutRequest {
    pageId: string
    layout: GridItem[]
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
 * Currently uses stub implementation - will be replaced with actual API calls
 */
export const layoutService = {
    /**
     * Save the current grid layout to the server
     * @param request Layout data to save
     * @returns Promise with save result
     */
    async saveLayout(request: SaveLayoutRequest): Promise<SaveLayoutResponse> {
        // Stub implementation - simulates API call with delay
        return new Promise((resolve) => {
            setTimeout(() => {
                // Simulate successful save
                const layoutId = `layout-${Date.now()}`

                // Log to console for debugging
                console.log('📦 Layout saved (stub):', {
                    layoutId,
                    pageId: request.pageId,
                    panelCount: request.layout.length,
                    layout: request.layout,
                    metadata: request.metadata
                })

                // Store in localStorage as temporary persistence
                try {
                    const savedLayouts = JSON.parse(localStorage.getItem('savedLayouts') || '[]')
                    savedLayouts.push({
                        id: layoutId,
                        ...request,
                        savedAt: new Date().toISOString()
                    })
                    localStorage.setItem('savedLayouts', JSON.stringify(savedLayouts))
                } catch (error) {
                    console.warn('Failed to store layout in localStorage:', error)
                }

                resolve({
                    success: true,
                    layoutId,
                    message: 'Layout saved successfully'
                })
            }, 500) // Simulate network delay
        })
    },

    /**
     * Load a saved layout from the server
     * @param layoutId The ID of the layout to load
     * @returns Promise with layout data
     */
    async loadLayout(layoutId: string): Promise<SaveLayoutRequest | null> {
        // Stub implementation
        return new Promise((resolve) => {
            setTimeout(() => {
                try {
                    const savedLayouts: SavedLayout[] = JSON.parse(localStorage.getItem('savedLayouts') || '[]')
                    const layout = savedLayouts.find((l: SavedLayout) => l.id === layoutId)

                    console.log('📦 Layout loaded (stub):', layout)
                    resolve(layout || null)
                } catch (error) {
                    console.warn('Failed to load layout from localStorage:', error)
                    resolve(null)
                }
            }, 300)
        })
    },

    /**
     * Get all saved layouts for a page
     * @param pageId The page ID to get layouts for
     * @returns Promise with array of layouts
     */
    async getLayoutsByPage(pageId: string): Promise<SaveLayoutRequest[]> {
        // Stub implementation
        return new Promise((resolve) => {
            setTimeout(() => {
                try {
                    const savedLayouts: SavedLayout[] = JSON.parse(localStorage.getItem('savedLayouts') || '[]')
                    const pageLayouts = savedLayouts.filter((l: SavedLayout) => l.pageId === pageId)

                    console.log('📦 Layouts for page (stub):', pageId, pageLayouts)
                    resolve(pageLayouts)
                } catch (error) {
                    console.warn('Failed to load layouts from localStorage:', error)
                    resolve([])
                }
            }, 300)
        })
    }
}
