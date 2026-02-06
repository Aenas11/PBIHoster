import { api } from './api'
import type {
    DatasetRefreshScheduleDto,
    CreateDatasetRefreshScheduleRequest,
    UpdateDatasetRefreshScheduleRequest,
    DatasetRefreshRunDto,
    ManualDatasetRefreshRequest
} from '../types/refresh'

export const refreshService = {
    async getSchedules(): Promise<DatasetRefreshScheduleDto[]> {
        return api.get<DatasetRefreshScheduleDto[]>('/refreshes/schedules')
    },

    async createSchedule(payload: CreateDatasetRefreshScheduleRequest): Promise<DatasetRefreshScheduleDto> {
        return api.post<DatasetRefreshScheduleDto>('/refreshes/schedules', payload)
    },

    async updateSchedule(id: string, payload: UpdateDatasetRefreshScheduleRequest): Promise<DatasetRefreshScheduleDto> {
        return api.put<DatasetRefreshScheduleDto>(`/refreshes/schedules/${id}`, payload)
    },

    async deleteSchedule(id: string): Promise<void> {
        await api.delete(`/refreshes/schedules/${id}`)
    },

    async toggleSchedule(id: string): Promise<DatasetRefreshScheduleDto> {
        return api.post<DatasetRefreshScheduleDto>(`/refreshes/schedules/${id}/toggle`)
    },

    async runDatasetRefresh(datasetId: string, payload: ManualDatasetRefreshRequest): Promise<DatasetRefreshRunDto> {
        return api.post<DatasetRefreshRunDto>(`/refreshes/datasets/${datasetId}/run`, payload)
    },

    async getHistory(datasetId: string, skip = 0, take = 50): Promise<DatasetRefreshRunDto[]> {
        const params = new URLSearchParams({ skip: skip.toString(), take: take.toString() })
        return api.get<DatasetRefreshRunDto[]>(`/refreshes/datasets/${datasetId}/history?${params.toString()}`)
    }
}
