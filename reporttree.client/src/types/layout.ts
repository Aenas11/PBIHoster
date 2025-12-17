/**
 * Extended grid item that includes component information
 */
export interface GridItemWithComponent {
    /** Unique identifier for this grid item */
    i: string
    /** X position in grid */
    x: number
    /** Y position in grid */
    y: number
    /** Width in grid units */
    w: number
    /** Height in grid units */
    h: number
    /** Minimum width */
    minW?: number
    /** Minimum height */
    minH?: number
    /** Maximum width */
    maxW?: number
    /** Maximum height */
    maxH?: number
    /** Whether item is static (not draggable/resizable) */
    static?: boolean
    
    /** Component type identifier */
    componentType: string
    /** Component-specific configuration data */
    componentConfig: Record<string, unknown>
    /** Optional metadata */
    metadata?: {
        title?: string
        description?: string
        createdAt?: string
        updatedAt?: string
    }
}

/**
 * Helper type for creating new grid items
 */
export type CreateGridItemInput = Pick<GridItemWithComponent, 'x' | 'y' | 'w' | 'h' | 'componentType' | 'componentConfig'> & {
    minW?: number
    minH?: number
    metadata?: GridItemWithComponent['metadata']
}
