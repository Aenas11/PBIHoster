type UsageEventType = 'page_view' | 'report_view' | 'widget_interaction'

interface UsageEventPayload {
  eventType: UsageEventType
  path?: string
  deviceType?: 'mobile' | 'tablet' | 'desktop' | 'unknown'
  metadata?: Record<string, string>
}

function detectDeviceType(): 'mobile' | 'tablet' | 'desktop' | 'unknown' {
  if (typeof window === 'undefined') return 'unknown'
  const width = window.innerWidth
  if (width < 768) return 'mobile'
  if (width < 1024) return 'tablet'
  return 'desktop'
}

async function sendEvents(events: UsageEventPayload[]) {
  if (events.length === 0) {
    return
  }

  const body = JSON.stringify({ events })

  try {
    if ('sendBeacon' in navigator) {
      const blob = new Blob([body], { type: 'application/json' })
      if (navigator.sendBeacon('/api/analytics/events', blob)) {
        return
      }
    }

    await fetch('/api/analytics/events', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body,
      credentials: 'same-origin'
    })
  } catch {
    // Best-effort analytics should never block UX.
  }
}

export async function trackPageView(path: string) {
  await sendEvents([
    {
      eventType: 'page_view',
      path,
      deviceType: detectDeviceType()
    }
  ])
}