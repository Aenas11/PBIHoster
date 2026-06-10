import { api } from './api'

type UsageEventType = 'page_view' | 'report_view' | 'widget_interaction'

interface UsageEventPayload {
  eventType: UsageEventType
  path?: string
  deviceType?: 'mobile' | 'tablet' | 'desktop' | 'unknown'
  metadata?: Record<string, string>
}

export interface EventTypeCount {
  eventType: string
  count: number
}

export interface PathCount {
  path: string
  count: number
}

export interface DailyEventCount {
  date: string
  totalEvents: number
  pageViews: number
  reportViews: number
  uniqueUsers: number
}

export interface DeviceTypeCount {
  deviceType: string
  count: number
}

export interface UsageSummaryResponse {
  totalEvents: number
  uniqueUsers: number
  eventTypes: EventTypeCount[]
  topPaths: PathCount[]
  dailySeries: DailyEventCount[]
  deviceTypes: DeviceTypeCount[]
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

async function trackEvent(event: UsageEventPayload) {
  await sendEvents([event])
}

export async function trackPageView(path: string) {
  await trackEvent({
    eventType: 'page_view',
    path,
    deviceType: detectDeviceType()
  })
}

export async function trackReportView(path: string, metadata?: Record<string, string>) {
  await trackEvent({
    eventType: 'report_view',
    path,
    deviceType: detectDeviceType(),
    metadata
  })
}

export async function trackWidgetInteraction(path: string, metadata?: Record<string, string>) {
  await trackEvent({
    eventType: 'widget_interaction',
    path,
    deviceType: detectDeviceType(),
    metadata
  })
}

export async function getAnalyticsSummary(days = 30): Promise<UsageSummaryResponse> {
  const safeDays = Math.min(90, Math.max(1, days))
  return api.get<UsageSummaryResponse>(`/analytics/summary?days=${safeDays}`)
}

export function getAnalyticsExportUrl(days = 30): string {
  const safeDays = Math.min(90, Math.max(1, days))
  return `/api/analytics/export?days=${safeDays}`
}