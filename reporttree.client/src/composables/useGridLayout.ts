import { ref, computed } from 'vue'
import type { Layout } from 'grid-layout-plus'
import { layoutService } from '../services/layoutService'
import type { GridItemWithComponent, CreateGridItemInput } from '../types/layout'
import { useComponentRegistry } from './useComponentRegistry'

const layout = ref<GridItemWithComponent[]>([])
const nextId = ref(0)
const isLoading = ref(false)

export function useGridLayout() {
    const { getComponent } = useComponentRegistry()

    /**
     * Add a new panel with component type
     */
    const addPanel = (componentType: string, customConfig?: Partial<CreateGridItemInput>) => {
        const componentDef = getComponent(componentType)

        if (!componentDef) {
            console.error(`Cannot add panel: Component type "${componentType}" not found`)
            return
        }

        const defaultDimensions = componentDef.defaultDimensions || { w: 4, h: 4 }
        const minDimensions = componentDef.minDimensions || { w: 2, h: 2 }

        const newItem: GridItemWithComponent = {
            i: `panel-${nextId.value++}`,
            x: customConfig?.x ?? ((layout.value.length * 2) % 12),
            y: customConfig?.y ?? (layout.value.length + 12),
            w: customConfig?.w ?? defaultDimensions.w,
            h: customConfig?.h ?? defaultDimensions.h,
            minW: customConfig?.minW ?? minDimensions.w,
            minH: customConfig?.minH ?? minDimensions.h,
            componentType,
            componentConfig: customConfig?.componentConfig ?? { ...componentDef.defaultConfig },
            metadata: {
                ...customConfig?.metadata,
                createdAt: new Date().toISOString()
            }
        }

        console.log('Adding new panel:', {
            id: newItem.i,
            type: newItem.componentType,
            position: { x: newItem.x, y: newItem.y },
            size: { w: newItem.w, h: newItem.h },
            hasConfig: !!newItem.componentConfig
        })

        layout.value.push(newItem)
        console.log('Total panels after add:', layout.value.length)
        return newItem
    }

    /**
     * Remove a panel by ID
     */
    const removePanel = (id: string) => {
        const index = layout.value.findIndex(item => item.i === id)
        if (index !== -1) {
            layout.value.splice(index, 1)
        }
    }

    /**
     * Update panel configuration
     */
    const updatePanelConfig = (id: string, config: Record<string, unknown>) => {
        const item = layout.value.find(item => item.i === id)
        if (item) {
            item.componentConfig = { ...item.componentConfig, ...config }
            if (!item.metadata) item.metadata = {}
            item.metadata.updatedAt = new Date().toISOString()
        }
    }

    /**
     * Get a specific panel by ID
     */
    const getPanel = (id: string): GridItemWithComponent | undefined => {
        return layout.value.find(item => item.i === id)
    }

    /**
     * Handler for layout-updated event from grid-layout-plus
     * Preserves component data when positions change
     */
    const onLayoutUpdated = (newLayout: Layout) => {
        // Create map of current items with their component data
        const dataMap = new Map<string, Pick<GridItemWithComponent, 'componentType' | 'componentConfig' | 'metadata'>>(
            layout.value.map(item => [
                item.i,
                {
                    componentType: item.componentType,
                    componentConfig: item.componentConfig,
                    metadata: item.metadata
                }
            ])
        )

        // Update layout positions while preserving component data
        newLayout.forEach((item, index) => {
            const data = dataMap.get(String(item.i))
            if (data && layout.value[index]) {
                // Merge position update with existing component data
                Object.assign(layout.value[index], {
                    ...item,
                    ...data
                })
            }
        })
    }

    /**
     * Update layout positions/sizes (from grid-layout-plus)
     * This is kept for potential future use with @layout-updated event
     */
    const updateLayout = (newLayout: Layout) => {
        // Preserve component data when updating layout positions
        const layoutMap = new Map(layout.value.map(item => [item.i, item]))

        layout.value = newLayout.map(layoutItem => {
            const existingItem = layoutMap.get(String(layoutItem.i))
            if (existingItem) {
                // Merge grid position updates with existing component data
                return {
                    ...existingItem,
                    x: layoutItem.x,
                    y: layoutItem.y,
                    w: layoutItem.w,
                    h: layoutItem.h,
                    minW: layoutItem.minW ?? existingItem.minW,
                    minH: layoutItem.minH ?? existingItem.minH,
                    maxW: layoutItem.maxW ?? existingItem.maxW,
                    maxH: layoutItem.maxH ?? existingItem.maxH,
                    static: layoutItem.static ?? existingItem.static
                } as GridItemWithComponent
            }
            return layoutItem as GridItemWithComponent
        })
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
                if (mostRecentLayout?.layout) {
                    layout.value = mostRecentLayout.layout

                    // Update nextId to avoid conflicts
                    const layoutToProcess = mostRecentLayout.layout
                    const maxId = layoutToProcess.reduce((max, item) => {
                        const match = item.i.match(/panel-(\d+)/)
                        const idStr = match?.[1]
                        if (idStr) {
                            const id = parseInt(idStr, 10)
                            return Math.max(max, id)
                        }
                        return max
                    }, -1)
                    nextId.value = maxId + 1
                }
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
        updatePanelConfig,
        getPanel,
        updateLayout,
        onLayoutUpdated,
        clearLayout,
        loadLayout
    }
}

// Export types for convenience
export type { GridItemWithComponent, CreateGridItemInput } from '../types/layout'
