import { api } from './api'
import type {
    WorkspaceDto,
    ReportDto,
    DashboardDto,
    EmbedTokenRequestDto,
    EmbedTokenResponseDto
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
    }
}
