using Microsoft.Identity.Client;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;
using ReportTree.Server.DTOs;
using ReportTree.Server.Models;
using System.Security.Cryptography.X509Certificates;

namespace ReportTree.Server.Services
{
    public class PowerBIService : IPowerBIService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PowerBIService> _logger;
        private IConfidentialClientApplication? _msalClient;
        private PowerBIConfiguration? _config;
        private DateTime _tokenExpiration;
        private string _accessToken = string.Empty;
        private readonly SemaphoreSlim _tokenLock = new SemaphoreSlim(1, 1);

        public PowerBIService(IConfiguration configuration, ILogger<PowerBIService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private void LoadConfiguration()
        {
            _config = new PowerBIConfiguration
            {
                TenantId = _configuration["PowerBI:TenantId"] ?? string.Empty,
                ClientId = _configuration["PowerBI:ClientId"] ?? string.Empty,
                ClientSecret = _configuration["PowerBI:ClientSecret"] ?? string.Empty,
                CertificateThumbprint = _configuration["PowerBI:CertificateThumbprint"] ?? string.Empty,
                CertificatePath = _configuration["PowerBI:CertificatePath"] ?? string.Empty,
                AuthorityUrl = _configuration["PowerBI:AuthorityUrl"] ?? "https://login.microsoftonline.com/{0}/",
                ResourceUrl = _configuration["PowerBI:ResourceUrl"] ?? "https://analysis.windows.net/powerbi/api",
                ApiUrl = _configuration["PowerBI:ApiUrl"] ?? "https://api.powerbi.com"
            };

            var authTypeStr = _configuration["PowerBI:AuthType"];
            if (Enum.TryParse<AuthenticationType>(authTypeStr, out var authType))
            {
                _config.AuthType = authType;
            }
            else
            {
                _config.AuthType = AuthenticationType.ClientSecret; // Default
            }
        }

        public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            await _tokenLock.WaitAsync(cancellationToken);
            try
            {
                if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiration > DateTime.UtcNow.AddMinutes(5))
                {
                    return _accessToken;
                }

                if (_config == null || string.IsNullOrEmpty(_config.ClientId))
                {
                    LoadConfiguration();
                }

                if (string.IsNullOrEmpty(_config!.ClientId) || string.IsNullOrEmpty(_config.TenantId))
                {
                    throw new InvalidOperationException("Power BI configuration is missing (ClientId or TenantId).");
                }

                if (_msalClient == null)
                {
                    var authority = string.Format(_config.AuthorityUrl, _config.TenantId);
                    var builder = ConfidentialClientApplicationBuilder.Create(_config.ClientId)
                        .WithAuthority(authority);

                    if (_config.AuthType == AuthenticationType.ClientSecret)
                    {
                        if (string.IsNullOrEmpty(_config.ClientSecret))
                            throw new InvalidOperationException("Power BI Client Secret is missing.");
                        builder.WithClientSecret(_config.ClientSecret);
                    }
                    else if (_config.AuthType == AuthenticationType.Certificate)
                    {
                        X509Certificate2? certificate = null;
                        if (!string.IsNullOrEmpty(_config.CertificatePath) && File.Exists(_config.CertificatePath))
                        {
                            certificate = X509CertificateLoader.LoadCertificateFromFile(_config.CertificatePath);
                        }
                        // Add logic for Thumbprint store lookup if needed
                        if(!string.IsNullOrEmpty(_config.CertificateThumbprint))
                        {
                            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                            store.Open(OpenFlags.ReadOnly);
                            var certs = store.Certificates.Find(X509FindType.FindByThumbprint, _config.CertificateThumbprint, validOnly: false);
                            if (certs.Count > 0)
                            {
                                certificate = certs[0];
                            }
                        }
                        
                        if (certificate == null)
                            throw new InvalidOperationException("Power BI Certificate not found.");
                            
                        builder.WithCertificate(certificate);
                    }

                    _msalClient = builder.Build();
                }

                var scopes = new[] { $"{_config.ResourceUrl}/.default" };
                var result = await _msalClient.AcquireTokenForClient(scopes).ExecuteAsync(cancellationToken);

                _accessToken = result.AccessToken;
                _tokenExpiration = result.ExpiresOn.DateTime;

                return _accessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acquire Power BI access token.");
                throw;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        private PowerBIClient CreatePowerBIClient(string accessToken)
        {
            var tokenCredentials = new TokenCredentials(accessToken, "Bearer");
            return new PowerBIClient(new Uri(_config?.ApiUrl ?? "https://api.powerbi.com"), tokenCredentials);
        }

        public async Task<IEnumerable<WorkspaceDto>> GetWorkspacesAsync(CancellationToken cancellationToken = default)
        {
            var token = await GetAccessTokenAsync(cancellationToken);
            using var client = CreatePowerBIClient(token);
            
            // Get groups (workspaces)
            var groups = await client.Groups.GetGroupsAsync(cancellationToken: cancellationToken);
            
            return groups.Value.Select(g => new WorkspaceDto
            {
                Id = g.Id,
                Name = g.Name
            });
        }

        public async Task<IEnumerable<ReportDto>> GetReportsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            var token = await GetAccessTokenAsync(cancellationToken);
            using var client = CreatePowerBIClient(token);

            var reports = await client.Reports.GetReportsInGroupAsync(workspaceId, cancellationToken: cancellationToken);

            return reports.Value.Select(r => new ReportDto
            {
                Id = r.Id,
                Name = r.Name,
                EmbedUrl = r.EmbedUrl,
                DatasetId = r.DatasetId
            });
        }

        public async Task<IEnumerable<DashboardDto>> GetDashboardsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            var token = await GetAccessTokenAsync(cancellationToken);
            using var client = CreatePowerBIClient(token);

            var dashboards = await client.Dashboards.GetDashboardsInGroupAsync(workspaceId, cancellationToken: cancellationToken);

            return dashboards.Value.Select(d => new DashboardDto
            {
                Id = d.Id,
                Name = d.DisplayName,
                EmbedUrl = d.EmbedUrl
            });
        }

        public async Task<ReportDto?> GetReportAsync(Guid workspaceId, Guid reportId, CancellationToken cancellationToken = default)
        {
            var token = await GetAccessTokenAsync(cancellationToken);
            using var client = CreatePowerBIClient(token);

            try 
            {
                var report = await client.Reports.GetReportInGroupAsync(workspaceId, reportId, cancellationToken: cancellationToken);
                return new ReportDto
                {
                    Id = report.Id,
                    Name = report.Name,
                    EmbedUrl = report.EmbedUrl,
                    DatasetId = report.DatasetId
                };
            }
            catch (HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<DashboardDto?> GetDashboardAsync(Guid workspaceId, Guid dashboardId, CancellationToken cancellationToken = default)
        {
            var token = await GetAccessTokenAsync(cancellationToken);
            using var client = CreatePowerBIClient(token);

            try
            {
                var dashboard = await client.Dashboards.GetDashboardInGroupAsync(workspaceId, dashboardId, cancellationToken: cancellationToken);
                return new DashboardDto
                {
                    Id = dashboard.Id,
                    Name = dashboard.DisplayName,
                    EmbedUrl = dashboard.EmbedUrl
                };
            }
            catch (HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<EmbedTokenResponseDto> GetReportEmbedTokenAsync(Guid workspaceId, Guid reportId, List<RLSIdentityDto>? identities = null, CancellationToken cancellationToken = default)
        {
            var token = await GetAccessTokenAsync(cancellationToken);
            using var client = CreatePowerBIClient(token);

            var report = await client.Reports.GetReportInGroupAsync(workspaceId, reportId, cancellationToken: cancellationToken);
            
            GenerateTokenRequestV2 request;

            if (identities != null && identities.Any())
            {
                var effectiveIdentities = identities.Select(i => new EffectiveIdentity(
                    username: i.Username,
                    roles: i.Roles,
                    datasets: i.Datasets
                )).ToList();

                request = new GenerateTokenRequestV2(
                    reports: new List<GenerateTokenRequestV2Report> { new GenerateTokenRequestV2Report(report.Id) },
                    identities: effectiveIdentities
                );
            }
            else
            {
                request = new GenerateTokenRequestV2(
                    reports: new List<GenerateTokenRequestV2Report> { new GenerateTokenRequestV2Report(report.Id) }
                );
            }
            request.Datasets = new List<GenerateTokenRequestV2Dataset> { new GenerateTokenRequestV2Dataset(report.DatasetId) };
            request.TargetWorkspaces = new List<GenerateTokenRequestV2TargetWorkspace> { new GenerateTokenRequestV2TargetWorkspace(workspaceId) };

            try
            {
                // Use V2 API for RLS support
                var embedTokenV2 = await client.EmbedToken.GenerateTokenAsync(request, cancellationToken: cancellationToken);

                return new EmbedTokenResponseDto
                {
                    AccessToken = embedTokenV2.Token,
                    EmbedUrl = report.EmbedUrl,
                    TokenId = embedTokenV2.TokenId.ToString(),
                    Expiration = embedTokenV2.Expiration
                };
            }
            catch (HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                _logger.LogWarning("Falling back to V1 Embed Token generation due to: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating V2 Embed Token, falling back to V1.");
            }
            // Fallback to V1 API if V2 fails (e.g., no RLS)
            var requestV1 = new GenerateTokenRequest(accessLevel: TokenAccessLevel.View);
            var embedToken = await client.Reports.GenerateTokenInGroupAsync(workspaceId, reportId, requestV1, cancellationToken: cancellationToken);
            return new EmbedTokenResponseDto
            {
                AccessToken = embedToken.Token,
                EmbedUrl = report.EmbedUrl,
                TokenId = embedToken.TokenId.ToString(),
                Expiration = embedToken.Expiration
            };


            // var embedToken = await client.EmbedToken.GenerateTokenAsync(request, cancellationToken: cancellationToken);

            // return new EmbedTokenResponseDto
            // {
            //     AccessToken = embedToken.Token,
            //     EmbedUrl = report.EmbedUrl,
            //     TokenId = embedToken.TokenId.ToString(),
            //     Expiration = embedToken.Expiration
            // };
        }

        public async Task<EmbedTokenResponseDto> GetDashboardEmbedTokenAsync(Guid workspaceId, Guid dashboardId, CancellationToken cancellationToken = default)
        {
            var token = await GetAccessTokenAsync(cancellationToken);
            using var client = CreatePowerBIClient(token);

            var dashboard = await client.Dashboards.GetDashboardInGroupAsync(workspaceId, dashboardId, cancellationToken: cancellationToken);


            // Fallback to V1 API
            var requestV1 = new GenerateTokenRequest(accessLevel: TokenAccessLevel.View);
            try 
            {
                var embedToken = await client.Dashboards.GenerateTokenInGroupAsync(workspaceId, dashboardId, requestV1, cancellationToken: cancellationToken);
            
            return new EmbedTokenResponseDto
            {
                AccessToken = embedToken.Token,
                EmbedUrl = dashboard.EmbedUrl,
                TokenId = embedToken.TokenId.ToString(),
                Expiration = embedToken.Expiration
            };
            }
            catch (HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                _logger.LogError("Error generating Dashboard Embed Token: {Message}", ex.Message);
                throw;
            }
        }
    }
}
