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
                provider.GroupClaimType = string.IsNullOrWhiteSpace(provider.GroupClaimType)
                    ? "groups"
                    : provider.GroupClaimType.Trim();
                provider.RoleClaimType = string.IsNullOrWhiteSpace(provider.RoleClaimType)
                    ? "roles"
                    : provider.RoleClaimType.Trim();

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

                if (!IsAllowedRole(provider.DefaultRole))
                {
                    throw new InvalidOperationException($"Security:ExternalAuth:Providers:{provider.Id}:DefaultRole must be one of Admin, Editor, Viewer.");
                }

                provider.Scopes = provider.Scopes
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                provider.DefaultRole = NormalizeRole(provider.DefaultRole);

                if (provider.GroupSyncEnabled)
                {
                    var mappingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var mappingTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var mapping in provider.GroupMappings)
                    {
                        mapping.ExternalGroup = mapping.ExternalGroup.Trim();
                        mapping.InternalGroup = mapping.InternalGroup.Trim();

                        if (string.IsNullOrWhiteSpace(mapping.ExternalGroup) || string.IsNullOrWhiteSpace(mapping.InternalGroup))
                        {
                            throw new InvalidOperationException($"Security:ExternalAuth:Providers:{provider.Id}:GroupMappings entries must include ExternalGroup and InternalGroup.");
                        }

                        if (!mappingKeys.Add(mapping.ExternalGroup))
                        {
                            throw new InvalidOperationException($"Security:ExternalAuth:Providers:{provider.Id}:GroupMappings has duplicate ExternalGroup '{mapping.ExternalGroup}'.");
                        }

                        mappingTargets.Add(mapping.InternalGroup);
                    }

                    if (provider.GroupMappings.Count == 0)
                    {
                        throw new InvalidOperationException($"Security:ExternalAuth:Providers:{provider.Id}:GroupSyncEnabled requires at least one GroupMappings entry.");
                    }
                }

                if (provider.RoleSyncEnabled)
                {
                    var roleKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var mapping in provider.RoleMappings)
                    {
                        mapping.ExternalRole = mapping.ExternalRole.Trim();
                        mapping.InternalRole = NormalizeRole(mapping.InternalRole);

                        if (string.IsNullOrWhiteSpace(mapping.ExternalRole) || string.IsNullOrWhiteSpace(mapping.InternalRole))
                        {
                            throw new InvalidOperationException($"Security:ExternalAuth:Providers:{provider.Id}:RoleMappings entries must include ExternalRole and InternalRole.");
                        }

                        if (!roleKeys.Add(mapping.ExternalRole))
                        {
                            throw new InvalidOperationException($"Security:ExternalAuth:Providers:{provider.Id}:RoleMappings has duplicate ExternalRole '{mapping.ExternalRole}'.");
                        }

                        if (!IsAllowedRole(mapping.InternalRole))
                        {
                            throw new InvalidOperationException($"Security:ExternalAuth:Providers:{provider.Id}:RoleMappings internal roles must be one of Admin, Editor, Viewer.");
                        }
                    }

                    if (provider.RoleMappings.Count == 0)
                    {
                        throw new InvalidOperationException($"Security:ExternalAuth:Providers:{provider.Id}:RoleSyncEnabled requires at least one RoleMappings entry.");
                    }
                }
            }

            normalized.Add(provider);
        }

        return normalized;
    }

    private static bool IsAllowedRole(string role) =>
        string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "Editor", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "Viewer", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeRole(string role)
    {
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return "Admin";
        }

        if (string.Equals(role, "Editor", StringComparison.OrdinalIgnoreCase))
        {
            return "Editor";
        }

        return "Viewer";
    }
}