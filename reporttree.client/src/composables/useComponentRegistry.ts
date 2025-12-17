import { componentRegistry } from '../plugins/componentRegistry'
import type { ComponentDefinition } from '../types/components'

/**
 * Composable for accessing the component registry
 */
export function useComponentRegistry() {
    /**
     * Get a component definition by type
     */
    const getComponent = (type: string): ComponentDefinition | undefined => {
        return componentRegistry.get(type)
    }

    /**
     * Check if a component type exists
     */
    const hasComponent = (type: string): boolean => {
        return componentRegistry.has(type)
    }

    /**
     * Get all available component definitions
     */
    const getAllComponents = (): ComponentDefinition[] => {
        return componentRegistry.getAll()
    }

    /**
     * Get all component types
     */
    const getComponentTypes = (): string[] => {
        return componentRegistry.getTypes()
    }

    /**
     * Register a new component (typically done at app initialization)
     */
    const registerComponent = (definition: ComponentDefinition): void => {
        componentRegistry.register(definition)
    }

    /**
     * Register multiple components at once
     */
    const registerComponents = (definitions: ComponentDefinition[]): void => {
        componentRegistry.registerAll(definitions)
    }

    return {
        getComponent,
        hasComponent,
        getAllComponents,
        getComponentTypes,
        registerComponent,
        registerComponents,
        // Access to the registry itself if needed
        registry: componentRegistry
    }
}
