import { ref, computed, onMounted, onUnmounted } from 'vue'
import { LAYOUT_CONFIG } from '../config/layout'

/**
 * Composable for managing responsive layout state
 */
export function useLayout() {
    const windowWidth = ref(typeof window !== 'undefined' ? window.innerWidth : 1200)
    const isSideNavExpanded = ref(windowWidth.value > LAYOUT_CONFIG.BREAKPOINTS.TABLET)

    // Computed properties
    const isDesktop = computed(() => windowWidth.value > LAYOUT_CONFIG.BREAKPOINTS.TABLET)
    const isMobile = computed(() => windowWidth.value <= LAYOUT_CONFIG.BREAKPOINTS.MOBILE)
    const isTablet = computed(() =>
        windowWidth.value > LAYOUT_CONFIG.BREAKPOINTS.MOBILE &&
        windowWidth.value <= LAYOUT_CONFIG.BREAKPOINTS.TABLET
    )

    // Fix: Side nav should be fixed on Tablet and Desktop to avoid overlaying content
    const isSideNavFixed = computed(() => windowWidth.value > LAYOUT_CONFIG.BREAKPOINTS.MOBILE)

    const contentMarginLeft = computed(() => {
        if (!isSideNavFixed.value) {
            return '0'
        }
        return isSideNavExpanded.value
            ? LAYOUT_CONFIG.SIDE_NAV.WIDTH_EXPANDED
            : LAYOUT_CONFIG.SIDE_NAV.WIDTH_COLLAPSED
    })

    // Debounced resize handler
    let resizeTimeout: ReturnType<typeof setTimeout> | null = null

    function handleResize() {
        if (resizeTimeout) {
            clearTimeout(resizeTimeout)
        }

        resizeTimeout = setTimeout(() => {
            windowWidth.value = window.innerWidth

            // Auto-adjust side nav based on screen size
            // When switching to mobile, ensure the nav is closed (collapsed)
            if (isMobile.value && isSideNavExpanded.value) {
                isSideNavExpanded.value = false
            }
        }, LAYOUT_CONFIG.DEBOUNCE.RESIZE_MS)
    }

    function toggleSideNav() {
        isSideNavExpanded.value = !isSideNavExpanded.value
    }

    function closeSideNavOnMobile() {
        if (!isDesktop.value) {
            isSideNavExpanded.value = false
        }
    }

    // Lifecycle
    onMounted(() => {
        if (typeof window !== 'undefined') {
            window.addEventListener('resize', handleResize)
        }
    })

    onUnmounted(() => {
        if (resizeTimeout) {
            clearTimeout(resizeTimeout)
        }
        if (typeof window !== 'undefined') {
            window.removeEventListener('resize', handleResize)
        }
    })

    return {
        // State
        windowWidth,
        isSideNavExpanded,

        // Computed
        isDesktop,
        isMobile,
        isTablet,
        isSideNavFixed,
        contentMarginLeft,

        // Methods
        toggleSideNav,
        closeSideNavOnMobile
    }
}
