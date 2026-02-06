using ReportTree.Server.DTOs;

namespace ReportTree.Server.Services
{
    public interface IPowerBIService
    {
        Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkspaceDto>> GetWorkspacesAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<ReportDto>> GetReportsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
        Task<IEnumerable<DashboardDto>> GetDashboardsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
        Task<IEnumerable<DatasetDto>> GetDatasetsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
        Task<EmbedTokenResponseDto> GetReportEmbedTokenAsync(Guid workspaceId, Guid reportId, List<RLSIdentityDto>? identities = null, CancellationToken cancellationToken = default);
        Task<EmbedTokenResponseDto> GetDashboardEmbedTokenAsync(Guid workspaceId, Guid dashboardId, CancellationToken cancellationToken = default);
        Task<ReportDto?> GetReportAsync(Guid workspaceId, Guid reportId, CancellationToken cancellationToken = default);
        Task<DashboardDto?> GetDashboardAsync(Guid workspaceId, Guid dashboardId, CancellationToken cancellationToken = default);
    }
}
