import { defineStore } from 'pinia'
import { ref } from 'vue'

export type ToastType = 'success' | 'error' | 'warning' | 'info'

export interface Toast {
    id: string
    type: ToastType
    title: string
    message?: string
    duration?: number
}

export const useToastStore = defineStore('toast', () => {
    const toasts = ref<Toast[]>([])

    function show(type: ToastType, title: string, message?: string, duration = 5000) {
        const id = `toast-${Date.now()}-${Math.random()}`
        const toast: Toast = { id, type, title, message, duration }
        toasts.value.push(toast)

        if (duration > 0) {
            setTimeout(() => {
                remove(id)
            }, duration)
        }
    }

    function success(title: string, message?: string, duration?: number) {
        show('success', title, message, duration)
    }

    function error(title: string, message?: string, duration?: number) {
        show('error', title, message, duration)
    }

    function warning(title: string, message?: string, duration?: number) {
        show('warning', title, message, duration)
    }

    function info(title: string, message?: string, duration?: number) {
        show('info', title, message, duration)
    }

    function remove(id: string) {
        const index = toasts.value.findIndex(t => t.id === id)
        if (index > -1) {
            toasts.value.splice(index, 1)
        }
    }

    return {
        toasts,
        show,
        success,
        error,
        warning,
        info,
        remove
    }
})
