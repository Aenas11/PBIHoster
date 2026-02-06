import { api } from './api'
import type {
    WorkspaceDto,
    ReportDto,
    DashboardDto,
    DatasetDto,
    EmbedTokenRequestDto,
    EmbedTokenResponseDto,
    PowerBIDiagnosticResultDto
} from '../types/powerbi'

export const powerBIService = {
    async getWorkspaces(): Promise<WorkspaceDto[]> {
        return api.get<WorkspaceDto[]>('/powerbi/workspaces')
    },

    async getReports(workspaceId: string): Promise<ReportDto[]> {
        return api.get<ReportDto[]>(`/powerbi/workspaces/${workspaceId}/reports`)
    },

    async getDashboards(workspaceId: string): Promise<DashboardDto[]> {
        return api.get<DashboardDto[]>(`/powerbi/workspaces/${workspaceId}/dashboards`)
    },

    async getDatasets(workspaceId: string): Promise<DatasetDto[]> {
        return api.get<DatasetDto[]>(`/powerbi/workspaces/${workspaceId}/datasets`)
    },

    async getReportEmbedToken(
        workspaceId: string,
        reportId: string,
        pageId?: number,
        enableRLS?: boolean,
        rlsRoles?: string[]
    ): Promise<EmbedTokenResponseDto> {
        const request: EmbedTokenRequestDto = {
            workspaceId,
            resourceId: reportId,
            resourceType: 'Report',
            pageId,
            enableRLS,
            rlsRoles
        }
        return api.post<EmbedTokenResponseDto>('/powerbi/embed/report', request)
    },

    async getDashboardEmbedToken(workspaceId: string, dashboardId: string, pageId?: number): Promise<EmbedTokenResponseDto> {
        const request: EmbedTokenRequestDto = {
            workspaceId,
            resourceId: dashboardId,
            resourceType: 'Dashboard',
            pageId
        }
        return api.post<EmbedTokenResponseDto>('/powerbi/embed/dashboard', request)
    },

    async runDiagnostics(workspaceId?: string, reportId?: string): Promise<PowerBIDiagnosticResultDto> {
        const params = new URLSearchParams()
        if (workspaceId) params.append('workspaceId', workspaceId)
        if (reportId) params.append('reportId', reportId)

        const query = params.toString()
        const suffix = query ? `?${query}` : ''
        return api.get<PowerBIDiagnosticResultDto>(`/powerbi/diagnostics${suffix}`)
    }
}
