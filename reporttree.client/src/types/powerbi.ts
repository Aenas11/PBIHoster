export interface WorkspaceDto {
    id: string;
    name: string;
}

export interface ReportDto {
    id: string;
    name: string;
    embedUrl: string;
    datasetId: string;
}

export interface DashboardDto {
    id: string;
    name: string;
    embedUrl: string;
}

export interface EmbedTokenRequestDto {
    workspaceId: string
    resourceId: string
    resourceType: 'Report' | 'Dashboard'
    pageId?: number
    enableRLS?: boolean
    rlsRoles?: string[]
}

export interface EmbedTokenResponseDto {
    accessToken: string;
    embedUrl: string;
    tokenId: string;
    expiration: string;
}

export interface RLSIdentityDto {
    username: string;
    roles: string[];
    datasets: string[];
}
