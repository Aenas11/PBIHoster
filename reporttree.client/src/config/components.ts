/**
 * Register all dashboard components
 * This file should be imported in main.ts before the app is created
 */
import { componentRegistry } from '../plugins/componentRegistry'
import type { ComponentDefinition } from '../types/components'
import { markRaw } from 'vue'

// Import components
import SimpleHtmlComponent from '../components/DashboardComponents/SimpleHtmlComponent.vue'
import SimpleHtmlComponentConfigure from '../components/DashboardComponents/SimpleHtmlComponentConfigure.vue'
import PowerBIReportComponent from '../components/DashboardComponents/PowerBIReportComponent.vue'
import PowerBIReportComponentConfigure from '../components/DashboardComponents/PowerBIReportComponentConfigure.vue'
import PowerBIDashboardComponent from '../components/DashboardComponents/PowerBIDashboardComponent.vue'
import PowerBIDashboardComponentConfigure from '../components/DashboardComponents/PowerBIDashboardComponentConfigure.vue'
import PowerBIWorkspaceComponent from '../components/DashboardComponents/PowerBIWorkspaceComponent.vue'
import PowerBIWorkspaceComponentConfigure from '../components/DashboardComponents/PowerBIWorkspaceComponentConfigure.vue'
import FavoritesComponent from '../components/DashboardComponents/FavoritesComponent.vue'
import FavoritesComponentConfigure from '../components/DashboardComponents/FavoritesComponentConfigure.vue'

// Define component configurations
const components: ComponentDefinition[] = [
    {
        type: 'simple-html',
        name: 'Simple HTML',
        description: 'Display static HTML content',
        component: markRaw(SimpleHtmlComponent),
        configComponent: markRaw(SimpleHtmlComponentConfigure),
        defaultConfig: {
            content: '<h3>Sample HTML Content</h3><p>Edit this component to customize the HTML.</p>'
        },
        defaultDimensions: { w: 4, h: 4 },
        minDimensions: { w: 2, h: 2 }
    },
    {
        type: 'power-bi-report',
        name: 'Power BI Report',
        description: 'Embed a Power BI report',
        component: markRaw(PowerBIReportComponent),
        configComponent: markRaw(PowerBIReportComponentConfigure),
        defaultConfig: {
            workspaceId: '',
            reportId: '',
            filterPaneEnabled: false,
            navContentPaneEnabled: false,
            background: 'Transparent',
            enableRLS: false,
            rlsRoles: []
        },
        defaultDimensions: { w: 12, h: 10 },
        minDimensions: { w: 4, h: 4 }
    },
    {
        type: 'power-bi-dashboard',
        name: 'Power BI Dashboard',
        description: 'Embed a Power BI dashboard',
        component: markRaw(PowerBIDashboardComponent),
        configComponent: markRaw(PowerBIDashboardComponentConfigure),
        defaultConfig: {
            workspaceId: '',
            dashboardId: '',
            filterPaneEnabled: false,
            background: 'Transparent',
            pageView: 'fitToWidth',
            locale: 'en-US'
        },
        defaultDimensions: { w: 12, h: 10 },
        minDimensions: { w: 4, h: 4 }
    },
    {
        type: 'power-bi-workspace',
        name: 'Power BI Workspace Reports',
        description: 'Display all reports from a Power BI workspace with tabs',
        component: markRaw(PowerBIWorkspaceComponent),
        configComponent: markRaw(PowerBIWorkspaceComponentConfigure),
        defaultConfig: {
            workspaceId: '',
            enableRLS: false,
            rlsRoles: []
        },
        defaultDimensions: { w: 12, h: 10 },
        minDimensions: { w: 6, h: 6 }
    },
    {
        type: 'favorites',
        name: 'Favorites & Recents',
        description: 'Show your favorite and recently viewed pages',
        component: markRaw(FavoritesComponent),
        configComponent: markRaw(FavoritesComponentConfigure),
        defaultConfig: {
            showFavorites: true,
            showRecents: true,
            maxItems: 6
        },
        defaultDimensions: { w: 6, h: 6 },
        minDimensions: { w: 3, h: 4 }
    }
]

/**
 * Initialize component registry with all dashboard components
 */
export function registerDashboardComponents() {
    componentRegistry.registerAll(components)
}
