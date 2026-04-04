using ReportTree.Server.DTOs;
using ReportTree.Server.Persistance;

namespace ReportTree.Server.Services;

public class OidcAuthService
{
    private readonly IExternalAuthProviderRepository _providerRepository;

    public OidcAuthService(IExternalAuthProviderRepository providerRepository)
    {
        _providerRepository = providerRepository;
    }

    public async Task<IReadOnlyList<ExternalAuthProviderSummaryResponse>> GetEnabledProvidersAsync()
    {
        var providers = await _providerRepository.GetEnabledAsync();

        return providers
            .Select(provider => new ExternalAuthProviderSummaryResponse(
                provider.Id,
                provider.DisplayName,
                provider.Scheme
            ))
            .ToList();
    }
}