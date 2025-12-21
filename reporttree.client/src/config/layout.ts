/**
 * Layout configuration constants
 */
export const LAYOUT_CONFIG = {
    BREAKPOINTS: {
        MOBILE: 768,
        TABLET: 1056,
        DESKTOP: 1312
    },
    SIDE_NAV: {
        WIDTH_EXPANDED: '16rem',
        WIDTH_COLLAPSED: '3rem'
    },
    HEADER: {
        HEIGHT: '3rem'
    },
    DEBOUNCE: {
        RESIZE_MS: 150
    }
} as const

export type LayoutConfig = typeof LAYOUT_CONFIG
