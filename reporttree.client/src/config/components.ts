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
    // Add more components here as they are developed
    // {
    //     type: 'power-bi-report',
    //     name: 'Power BI Report',
    //     description: 'Embed a Power BI report',
    //     component: markRaw(PowerBIReportComponent),
    //     configComponent: markRaw(PowerBIReportConfigure),
    //     defaultConfig: {
    //         reportId: '',
    //         workspaceId: ''
    //     },
    //     defaultDimensions: { w: 6, h: 8 },
    //     minDimensions: { w: 4, h: 4 }
    // }
]

/**
 * Initialize component registry with all dashboard components
 */
export function registerDashboardComponents() {
    componentRegistry.registerAll(components)
}
