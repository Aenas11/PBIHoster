using ReportTree.Server.DTOs;

namespace ReportTree.Server.Services;

public class OidcAuthService
{
    private readonly ExternalAuthConfigurationService _externalAuthConfigurationService;

    public OidcAuthService(ExternalAuthConfigurationService externalAuthConfigurationService)
    {
        _externalAuthConfigurationService = externalAuthConfigurationService;
    }

    public async Task<IReadOnlyList<ExternalAuthProviderSummaryResponse>> GetEnabledProvidersAsync()
    {
        var providers = (await _externalAuthConfigurationService.GetEffectiveProvidersAsync())
            .Where(p => p.Enabled)
            .ToList();

        return providers
            .Select(provider => new ExternalAuthProviderSummaryResponse(
                provider.Id,
                provider.DisplayName,
                provider.Scheme
            ))
            .ToList();
    }
}