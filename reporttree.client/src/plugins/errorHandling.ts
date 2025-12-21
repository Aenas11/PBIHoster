import type { App, ComponentPublicInstance } from 'vue'
import { reportClientError } from '../services/monitoring'
import { useToastStore } from '../stores/toast'

export function installErrorHandling(app: App) {
  app.config.errorHandler = async (err: unknown, _instance: ComponentPublicInstance | null, info: string) => {
    const errorMessage = err instanceof Error ? err.message : String(err)
    const toastStore = useToastStore()

    toastStore.error('Unexpected error', 'Something went wrong while rendering the page.')

    await reportClientError({
      message: errorMessage,
      stack: err instanceof Error ? err.stack : undefined,
      info
    })

    console.error(err)
  }

  window.addEventListener('unhandledrejection', async (event) => {
    const reason = event.reason ?? 'Unhandled promise rejection'
    const message = reason instanceof Error ? reason.message : String(reason)
    await reportClientError({
      message,
      stack: reason?.stack,
      info: 'unhandledrejection'
    })
  })
}
