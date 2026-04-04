using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance;

public class ConfigurationExternalAuthProviderRepository : IExternalAuthProviderRepository
{
    private readonly IReadOnlyList<ExternalAuthProvider> _providers;

    public ConfigurationExternalAuthProviderRepository(IConfiguration configuration)
    {
        var options = new ExternalAuthOptions();
        configuration.Bind("Security:ExternalAuth", options);
        _providers = ValidateAndNormalize(options.Providers);
    }

    public Task<IReadOnlyList<ExternalAuthProvider>> GetAllAsync() => Task.FromResult(_providers);

    public Task<IReadOnlyList<ExternalAuthProvider>> GetEnabledAsync() =>
        Task.FromResult<IReadOnlyList<ExternalAuthProvider>>(_providers.Where(p => p.Enabled).ToList());

    public Task<ExternalAuthProvider?> GetByIdAsync(string id)
    {
        var provider = _providers.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(provider);
    }

    private static IReadOnlyList<ExternalAuthProvider> ValidateAndNormalize(IEnumerable<ExternalAuthProvider> providers)
    {
        var normalized = new List<ExternalAuthProvider>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var provider in providers)
        {
            provider.Id = provider.Id.Trim();
            provider.DisplayName = provider.DisplayName.Trim();
            provider.Authority = provider.Authority.Trim();
            provider.ClientId = provider.ClientId.Trim();
            provider.ClientSecret = provider.ClientSecret?.Trim() ?? string.Empty;
            provider.CallbackPath = provider.GetCallbackPathOrDefault();

            if (string.IsNullOrWhiteSpace(provider.Id))
            {
                throw new InvalidOperationException("Security:ExternalAuth:Providers[*]:Id is required.");
            }

            if (!ids.Add(provider.Id))
            {
                throw new InvalidOperationException($"Duplicate external auth provider id: {provider.Id}");
            }

            if (provider.Enabled)
            {
                if (string.IsNullOrWhiteSpace(provider.DisplayName))
                {
                    throw new InvalidOperationException($"Security:ExternalAuth:Providers:{provider.Id}:DisplayName is required.");
                }

                if (string.IsNullOrWhiteSpace(provider.Authority))
                {
                    throw new InvalidOperationException($"Security:ExternalAuth:Providers:{provider.Id}:Authority is required.");
                }

                if (string.IsNullOrWhiteSpace(provider.ClientId))
                {
                    throw new InvalidOperationException($"Security:ExternalAuth:Providers:{provider.Id}:ClientId is required.");
                }

                if (!provider.CallbackPath.StartsWith('/'))
                {
                    throw new InvalidOperationException($"Security:ExternalAuth:Providers:{provider.Id}:CallbackPath must start with '/'.");
                }

                if (provider.Scopes.Count == 0 || !provider.Scopes.Any(s => string.Equals(s, "openid", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"Security:ExternalAuth:Providers:{provider.Id}:Scopes must include 'openid'.");
                }

                provider.Scopes = provider.Scopes
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            normalized.Add(provider);
        }

        return normalized;
    }
}