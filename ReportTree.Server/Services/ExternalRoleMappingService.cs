using System.Security.Claims;
using ReportTree.Server.Models;

namespace ReportTree.Server.Services;

public class ExternalRoleMappingService
{
    public List<string> ResolveRoles(ExternalAuthProvider provider, ClaimsPrincipal principal)
    {
        if (!provider.RoleSyncEnabled)
        {
            return new List<string> { NormalizeRole(provider.DefaultRole) };
        }

        var externalRoles = principal
            .Claims
            .Where(c => string.Equals(c.Type, provider.RoleClaimType, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value?.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mappedRoles = provider.RoleMappings
            .Where(mapping => externalRoles.Contains(mapping.ExternalRole))
            .Select(mapping => NormalizeRole(mapping.InternalRole))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (mappedRoles.Count > 0)
        {
            return mappedRoles;
        }

        return new List<string> { NormalizeRole(provider.DefaultRole) };
    }

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