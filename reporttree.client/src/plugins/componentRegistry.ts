import type { ComponentDefinition } from '../types/components'

/**
 * Global component registry for dashboard components
 */
export class ComponentRegistry {
    private components = new Map<string, ComponentDefinition>()

    /**
     * Register a new component type
     */
    register(definition: ComponentDefinition): void {
        if (this.components.has(definition.type)) {
            console.warn(`Component type "${definition.type}" is already registered. Overwriting.`)
        }
        this.components.set(definition.type, definition)
    }

    /**
     * Register multiple components at once
     */
    registerAll(definitions: ComponentDefinition[]): void {
        definitions.forEach(def => this.register(def))
    }

    /**
     * Get a component definition by type
     */
    get(type: string): ComponentDefinition | undefined {
        return this.components.get(type)
    }

    /**
     * Check if a component type is registered
     */
    has(type: string): boolean {
        return this.components.has(type)
    }

    /**
     * Get all registered component definitions
     */
    getAll(): ComponentDefinition[] {
        return Array.from(this.components.values())
    }

    /**
     * Get all component types
     */
    getTypes(): string[] {
        return Array.from(this.components.keys())
    }

    /**
     * Unregister a component type
     */
    unregister(type: string): boolean {
        return this.components.delete(type)
    }

    /**
     * Clear all registered components
     */
    clear(): void {
        this.components.clear()
    }

    /**
     * Get component count
     */
    get size(): number {
        return this.components.size
    }
}

// Global singleton instance
export const componentRegistry = new ComponentRegistry()
