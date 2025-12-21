interface MonitoringPayload {
  message: string
  stack?: string
  info?: string
  context?: Record<string, unknown>
  correlationId?: string
  kind: 'api' | 'client'
}

const monitoringEndpoint = import.meta.env.VITE_MONITORING_ENDPOINT as string | undefined

async function sendToSink(payload: MonitoringPayload) {
  if (!monitoringEndpoint) {
    console.error('[monitoring] sink not configured', payload)
    return
  }

  try {
    const body = JSON.stringify({
      ...payload,
      timestamp: new Date().toISOString(),
      userAgent: window.navigator.userAgent
    })

    if ('sendBeacon' in navigator) {
      const success = navigator.sendBeacon(monitoringEndpoint, body)
      if (success) return
    }

    await fetch(monitoringEndpoint, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body
    })
  } catch (error) {
    console.error('[monitoring] failed to report error', error)
  }
}

export async function reportApiError(options: {
  message: string
  path: string
  method: string
  status?: number
  correlationId?: string
  responseBody?: string
}) {
  await sendToSink({
    message: options.message,
    stack: options.responseBody,
    info: `${options.method} ${options.path}`,
    correlationId: options.correlationId,
    context: { status: options.status },
    kind: 'api'
  })
}

export async function reportClientError(options: {
  message: string
  stack?: string
  info?: string
}) {
  await sendToSink({
    ...options,
    kind: 'client'
  })
}
