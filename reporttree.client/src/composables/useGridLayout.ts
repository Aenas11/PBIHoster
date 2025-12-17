import { ref, computed } from 'vue'
import type { Layout } from 'grid-layout-plus'
import { layoutService } from '../services/layoutService'

export interface GridItem {
    i: string
    x: number
    y: number
    w: number
    h: number
    minW?: number
    minH?: number
    maxW?: number
    maxH?: number
    static?: boolean
}

const layout = ref<GridItem[]>([])
const nextId = ref(0)
const isLoading = ref(false)

export function useGridLayout() {
    const addPanel = () => {
        const newItem: GridItem = {
            i: `panel-${nextId.value++}`,
            x: (layout.value.length * 2) % 12,
            y: layout.value.length + 12,
            w: 4,
            h: 4,
            minW: 2,
            minH: 2
        }
        layout.value.push(newItem)
    }

    const removePanel = (id: string) => {
        const index = layout.value.findIndex(item => item.i === id)
        if (index !== -1) {
            layout.value.splice(index, 1)
        }
    }

    const updateLayout = (newLayout: Layout) => {
        layout.value = newLayout as GridItem[]
    }

    const clearLayout = () => {
        layout.value = []
        nextId.value = 0
    }

    // Load layout from API using layoutService
    const loadLayout = async (pageId: string) => {
        isLoading.value = true
        try {
            // Get all layouts for this page
            const layouts = await layoutService.getLayoutsByPage(pageId)

            if (layouts.length > 0) {
                // Use the most recent layout (last one in the array)
                const mostRecentLayout = layouts[layouts.length - 1]
                layout.value = mostRecentLayout.layout

                // Update nextId to avoid conflicts
                const maxId = mostRecentLayout.layout.reduce((max, item) => {
                    const match = item.i.match(/panel-(\d+)/)
                    if (match) {
                        const id = parseInt(match[1])
                        return Math.max(max, id)
                    }
                    return max
                }, -1)
                nextId.value = maxId + 1
            } else {
                // No saved layout found, start with empty layout
                console.log(`No saved layout found for page ${pageId}`)
                clearLayout()
            }
        } catch (error) {
            console.error('Failed to load page layout:', error)
            // Fall back to empty layout on error
            clearLayout()
        } finally {
            isLoading.value = false
        }
    }

    return {
        layout: computed(() => layout.value),
        isLoading: computed(() => isLoading.value),
        addPanel,
        removePanel,
        updateLayout,
        clearLayout,
        loadLayout
    }
}
