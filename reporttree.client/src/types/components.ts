import type { Component } from 'vue'

/**
 * Component definition for the registry
 */
export interface ComponentDefinition {
    /** Unique identifier for the component type */
    type: string
    /** Display name */
    name: string
    /** Description for UI/tooltips */
    description: string
    /** Icon component (optional) */
    icon?: Component
    /** The main component to render */
    component: Component
    /** Configuration component for settings (optional) */
    configComponent?: Component
    /** Default configuration when creating new instance */
    defaultConfig: Record<string, unknown>
    /** Minimum grid dimensions */
    minDimensions?: {
        w: number
        h: number
    }
    /** Default grid dimensions */
    defaultDimensions?: {
        w: number
        h: number
    }
}

/**
 * Props that all dashboard components must accept
 */
export interface DashboardComponentProps {
    /** Component-specific configuration data */
    config: Record<string, unknown>
    /** Unique identifier for this component instance */
    id: string
    /** Grid item dimensions (for responsive behavior) */
    dimensions?: {
        w: number
        h: number
    }
}

/**
 * Power BI Report Component Configuration
 */
export interface PowerBIReportComponentConfig {
    workspaceId?: string
    reportId?: string
    enableRLS?: boolean
    rlsRoles?: string[] // RLS roles to apply for the current user
}

/**
 * Power BI Dashboard Component Configuration
 */
export interface PowerBIDashboardComponentConfig {
    workspaceId?: string
    dashboardId?: string
}

/**
 * Power BI Workspace Component Configuration
 * Displays all reports from a workspace with tabs
 */
export interface PowerBIWorkspaceComponentConfig {
    workspaceId?: string
    enableRLS?: boolean
    rlsRoles?: string[]
}

/**
 * Favorites component configuration
 */
export interface FavoritesComponentConfig {
    showFavorites?: boolean
    showRecents?: boolean
    maxItems?: number
}

/**
 * Props for configuration components
 */
export interface ComponentConfigProps {
    /** Current configuration */
    modelValue: Record<string, unknown>
    /** Component type being configured */
    componentType: string
}
