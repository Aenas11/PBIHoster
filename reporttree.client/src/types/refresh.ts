export type RefreshStatus = 'Queued' | 'InProgress' | 'Succeeded' | 'Failed' | 'Cancelled'

export interface RefreshNotificationTargetDto {
    type: string
    target: string
}

export interface DatasetRefreshScheduleDto {
    id: string
    name: string
    workspaceId: string
    datasetId: string
    reportId?: string | null
    pageId?: number | null
    enabled: boolean
    cron: string
    timeZone: string
    retryCount: number
    retryBackoffSeconds: number
    notifyOnSuccess: boolean
    notifyOnFailure: boolean
    notifyTargets: RefreshNotificationTargetDto[]
    createdByUserId: string
    createdAtUtc: string
    updatedAtUtc: string
}

export interface CreateDatasetRefreshScheduleRequest {
    name: string
    workspaceId: string
    datasetId: string
    reportId?: string | null
    pageId?: number | null
    enabled: boolean
    cron: string
    timeZone: string
    retryCount?: number | null
    retryBackoffSeconds?: number | null
    notifyOnSuccess: boolean
    notifyOnFailure: boolean
    notifyTargets?: RefreshNotificationTargetDto[]
}

export type UpdateDatasetRefreshScheduleRequest = CreateDatasetRefreshScheduleRequest

export interface ManualDatasetRefreshRequest {
    workspaceId: string
    reportId?: string | null
    pageId?: number | null
}

export interface DatasetRefreshRunDto {
    id: string
    scheduleId?: string | null
    workspaceId: string
    datasetId: string
    reportId?: string | null
    pageId?: number | null
    triggeredByUserId?: string | null
    requestedAtUtc: string
    startedAtUtc?: string | null
    completedAtUtc?: string | null
    status: RefreshStatus
    failureReason?: string | null
    powerBiRequestId?: string | null
    powerBiActivityId?: string | null
    retriesAttempted: number
    durationMs?: number | null
}
