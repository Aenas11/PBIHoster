import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { useAuthStore } from './auth'

/**
 * Global Edit Mode Store
 * 
 * Manages the edit mode state across the application.
 * Edit mode allows users with appropriate permissions (Admin/Editor) to:
 * - Edit page structure and navigation
 * - Drag and resize components on pages
 * - Access the tools panel for component management
 * 
 * Best Practices:
 * - Centralized state management for edit mode
 * - Permission-based access control
 * - Automatic exit on route changes for safety
 * - Clear separation between view and edit modes
 */
export const useEditModeStore = defineStore('editMode', () => {
    const authStore = useAuthStore()

    // State
    const isEditMode = ref(false)

    // Computed
    const canEdit = computed(() => {
        return authStore.roles.includes('Admin') || authStore.roles.includes('Editor')
    })

    // Actions
    function enterEditMode() {
        if (!canEdit.value) {
            console.warn('User does not have permission to enter edit mode')
            return false
        }
        isEditMode.value = true
        return true
    }

    function exitEditMode() {
        isEditMode.value = false
    }

    function toggleEditMode() {
        if (isEditMode.value) {
            exitEditMode()
        } else {
            return enterEditMode()
        }
    }

    /**
     * Force exit edit mode (e.g., on logout or route change)
     */
    function forceExitEditMode() {
        isEditMode.value = false
    }

    return {
        isEditMode: computed(() => isEditMode.value),
        canEdit,
        enterEditMode,
        exitEditMode,
        toggleEditMode,
        forceExitEditMode
    }
})
