using System.Text.Json;
using ReportTree.Server.DTOs;
using ReportTree.Server.Models;
using ReportTree.Server.Persistance;

namespace ReportTree.Server.Services;

public class ExternalAuthConfigurationService
{
    private const string OverridesSettingKey = "Security.ExternalAuth.ProviderOverrides";

    private readonly IExternalAuthProviderRepository _providerRepository;
    private readonly SettingsService _settingsService;

    public ExternalAuthConfigurationService(IExternalAuthProviderRepository providerRepository, SettingsService settingsService)
    {
        _providerRepository = providerRepository;
        _settingsService = settingsService;
    }

    public async Task<IReadOnlyList<ExternalAuthProvider>> GetEffectiveProvidersAsync()
    {
        var baseProviders = (await _providerRepository.GetAllAsync()).ToList();
        var overrides = await LoadOverridesAsync();

        foreach (var provider in baseProviders)
        {
            if (!overrides.TryGetValue(provider.Id, out var providerOverride))
            {
                continue;
            }

            provider.DefaultRole = NormalizeRole(providerOverride.DefaultRole, provider.DefaultRole);
            provider.GroupSyncEnabled = providerOverride.GroupSyncEnabled;
            provider.GroupClaimType = string.IsNullOrWhiteSpace(providerOverride.GroupClaimType)
                ? provider.GroupClaimType
                : providerOverride.GroupClaimType.Trim();
            provider.RemoveUnmappedGroupMemberships = providerOverride.RemoveUnmappedGroupMemberships;
            provider.GroupMappings = providerOverride.GroupMappings
                .Select(m => new ExternalGroupMapping
                {
                    ExternalGroup = m.ExternalGroup.Trim(),
                    InternalGroup = m.InternalGroup.Trim()
                })
                .Where(m => !string.IsNullOrWhiteSpace(m.ExternalGroup) && !string.IsNullOrWhiteSpace(m.InternalGroup))
                .ToList();

            provider.RoleSyncEnabled = providerOverride.RoleSyncEnabled;
            provider.RoleClaimType = string.IsNullOrWhiteSpace(providerOverride.RoleClaimType)
                ? provider.RoleClaimType
                : providerOverride.RoleClaimType.Trim();
            provider.RoleMappings = providerOverride.RoleMappings
                .Select(m => new ExternalRoleMapping
                {
                    ExternalRole = m.ExternalRole.Trim(),
                    InternalRole = NormalizeRole(m.InternalRole, "Viewer")
                })
                .Where(m => !string.IsNullOrWhiteSpace(m.ExternalRole) && !string.IsNullOrWhiteSpace(m.InternalRole))
                .ToList();
        }

        return baseProviders;
    }

    public async Task<IReadOnlyList<ExternalAuthAdminConfigResponse>> GetAdminConfigsAsync()
    {
        var providers = await GetEffectiveProvidersAsync();
        return providers.Select(ToAdminConfig).ToList();
    }

    public async Task<ExternalAuthProvider?> GetEffectiveProviderByIdAsync(string providerId)
    {
        var providers = await GetEffectiveProvidersAsync();
        return providers.FirstOrDefault(p => string.Equals(p.Id, providerId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task SaveAdminConfigsAsync(ExternalAuthAdminConfigUpdateRequest request, string modifiedBy)
    {
        var baseProviders = await _providerRepository.GetAllAsync();
        var knownProviders = baseProviders
            .Select(p => p.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var overrides = new Dictionary<string, ExternalProviderOverride>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in request.Providers)
        {
            if (!knownProviders.Contains(provider.ProviderId))
            {
                continue;
            }

            overrides[provider.ProviderId] = new ExternalProviderOverride
            {
                DefaultRole = NormalizeRole(provider.DefaultRole, "Viewer"),
                GroupSyncEnabled = provider.GroupSyncEnabled,
                GroupClaimType = provider.GroupClaimType?.Trim() ?? "groups",
                RemoveUnmappedGroupMemberships = provider.RemoveUnmappedGroupMemberships,
                GroupMappings = provider.GroupMappings
                    .Select(m => new ExternalGroupMappingDto(m.ExternalGroup.Trim(), m.InternalGroup.Trim()))
                    .Where(m => !string.IsNullOrWhiteSpace(m.ExternalGroup) && !string.IsNullOrWhiteSpace(m.InternalGroup))
                    .DistinctBy(m => m.ExternalGroup, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                RoleSyncEnabled = provider.RoleSyncEnabled,
                RoleClaimType = provider.RoleClaimType?.Trim() ?? "roles",
                RoleMappings = provider.RoleMappings
                    .Select(m => new ExternalRoleMappingDto(m.ExternalRole.Trim(), NormalizeRole(m.InternalRole, "Viewer")))
                    .Where(m => !string.IsNullOrWhiteSpace(m.ExternalRole) && !string.IsNullOrWhiteSpace(m.InternalRole))
                    .DistinctBy(m => m.ExternalRole, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };
        }

        var payload = JsonSerializer.Serialize(overrides);
        await _settingsService.UpsertSettingAsync(
            OverridesSettingKey,
            payload,
            "Authentication",
            "Non-secret external auth mapping overrides",
            false,
            modifiedBy
        );
    }

    private async Task<Dictionary<string, ExternalProviderOverride>> LoadOverridesAsync()
    {
        var json = await _settingsService.GetValueAsync(OverridesSettingKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, ExternalProviderOverride>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, ExternalProviderOverride>>(json)
                   ?? new Dictionary<string, ExternalProviderOverride>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, ExternalProviderOverride>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static ExternalAuthAdminConfigResponse ToAdminConfig(ExternalAuthProvider provider) =>
        new(
            provider.Id,
            provider.DisplayName,
            provider.Enabled,
            provider.DefaultRole,
            provider.GroupSyncEnabled,
            provider.GroupClaimType,
            provider.RemoveUnmappedGroupMemberships,
            provider.GroupMappings.Select(m => new ExternalGroupMappingDto(m.ExternalGroup, m.InternalGroup)).ToList(),
            provider.RoleSyncEnabled,
            provider.RoleClaimType,
            provider.RoleMappings.Select(m => new ExternalRoleMappingDto(m.ExternalRole, m.InternalRole)).ToList()
        );

    private static string NormalizeRole(string role, string fallback)
    {
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)) return "Admin";
        if (string.Equals(role, "Editor", StringComparison.OrdinalIgnoreCase)) return "Editor";
        if (string.Equals(role, "Viewer", StringComparison.OrdinalIgnoreCase)) return "Viewer";
        return fallback;
    }

    private sealed class ExternalProviderOverride
    {
        public string DefaultRole { get; set; } = "Viewer";
        public bool GroupSyncEnabled { get; set; }
        public string GroupClaimType { get; set; } = "groups";
        public bool RemoveUnmappedGroupMemberships { get; set; }
        public List<ExternalGroupMappingDto> GroupMappings { get; set; } = new();
        public bool RoleSyncEnabled { get; set; }
        public string RoleClaimType { get; set; } = "roles";
        public List<ExternalRoleMappingDto> RoleMappings { get; set; } = new();
    }
}