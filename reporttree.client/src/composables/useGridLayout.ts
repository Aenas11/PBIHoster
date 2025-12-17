import { ref, computed } from 'vue'
import type { Layout } from 'grid-layout-plus'

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

    return {
        layout: computed(() => layout.value),
        addPanel,
        removePanel,
        updateLayout,
        clearLayout
    }
}
