using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ReportTree.Server.DTOs;
using ReportTree.Server.Services;
using Xunit;

namespace ReportTree.Server.Tests;

public class PowerBIDiagnosticsServiceTests
{
    [Fact]
    public async Task RunAsync_ReturnsSuccess_WithSandboxData()
    {
        var fakeService = FakePowerBIService.CreateWithSampleData();
        var configuration = BuildConfig();
        var diagnostics = new PowerBIDiagnosticsService(fakeService, configuration, NullLogger<PowerBIDiagnosticsService>.Instance);

        var result = await diagnostics.RunAsync();

        Assert.True(result.Success);
        Assert.NotNull(result.WorkspaceId);
        Assert.NotNull(result.ReportId);
        Assert.True(fakeService.EmbedTokenRequested);
        Assert.Contains(result.Checks, c => c.Name == "Embed token generation" && c.Status == DiagnosticStatus.Success);
    }

    [Fact]
    public async Task RunAsync_Fails_WhenConfigurationMissing()
    {
        var fakeService = FakePowerBIService.CreateWithSampleData();
        var emptyConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var diagnostics = new PowerBIDiagnosticsService(fakeService, emptyConfig, NullLogger<PowerBIDiagnosticsService>.Instance);

        var result = await diagnostics.RunAsync();

        Assert.False(result.Success);
        Assert.Contains(result.Checks, c => c.Name == "Configuration" && c.Status == DiagnosticStatus.Error);
    }

    private static IConfiguration BuildConfig()
    {
        var values = new Dictionary<string, string?>
        {
            ["PowerBI:TenantId"] = "sandbox-tenant",
            ["PowerBI:ClientId"] = Guid.NewGuid().ToString(),
            ["PowerBI:ClientSecret"] = "sandbox-secret",
            ["PowerBI:AuthType"] = "ClientSecret"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}

file class FakePowerBIService : IPowerBIService
{
    public List<WorkspaceDto> Workspaces { get; init; } = new();
    public Dictionary<Guid, List<ReportDto>> ReportsByWorkspace { get; init; } = new();
    public bool EmbedTokenRequested { get; private set; }

    public static FakePowerBIService CreateWithSampleData()
    {
        var workspaceId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        return new FakePowerBIService
        {
            Workspaces = new List<WorkspaceDto>
            {
                new WorkspaceDto { Id = workspaceId, Name = "Sandbox Workspace" }
            },
            ReportsByWorkspace = new Dictionary<Guid, List<ReportDto>>
            {
                [workspaceId] = new List<ReportDto>
                {
                    new ReportDto
                    {
                        Id = reportId,
                        Name = "Sample Report",
                        DatasetId = Guid.NewGuid().ToString(),
                        EmbedUrl = "https://powerbi.microsoft.com"
                    }
                }
            }
        };
    }

    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult("sandbox-token");
    }

    public Task<IEnumerable<WorkspaceDto>> GetWorkspacesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<WorkspaceDto>>(Workspaces);
    }

    public Task<IEnumerable<ReportDto>> GetReportsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ReportDto>>(ReportsByWorkspace.GetValueOrDefault(workspaceId, new List<ReportDto>()));
    }

    public Task<IEnumerable<DashboardDto>> GetDashboardsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<DashboardDto>>(Array.Empty<DashboardDto>());
    }

    public Task<EmbedTokenResponseDto> GetReportEmbedTokenAsync(Guid workspaceId, Guid reportId, List<RLSIdentityDto>? identities = null, CancellationToken cancellationToken = default)
    {
        EmbedTokenRequested = true;
        return Task.FromResult(new EmbedTokenResponseDto
        {
            AccessToken = "embed-token",
            EmbedUrl = "https://powerbi.microsoft.com",
            TokenId = Guid.NewGuid().ToString(),
            Expiration = DateTime.UtcNow.AddMinutes(30)
        });
    }

    public Task<EmbedTokenResponseDto> GetDashboardEmbedTokenAsync(Guid workspaceId, Guid dashboardId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<ReportDto?> GetReportAsync(Guid workspaceId, Guid reportId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ReportsByWorkspace.GetValueOrDefault(workspaceId, new List<ReportDto>()).FirstOrDefault(r => r.Id == reportId));
    }

    public Task<DashboardDto?> GetDashboardAsync(Guid workspaceId, Guid dashboardId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<DashboardDto?>(null);
    }

    public Task<IEnumerable<DatasetDto>> GetDatasetsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<DatasetDto>>(Array.Empty<DatasetDto>());
        
    }
}
