using System.Security.Claims;
using ReportTree.Server.Models;
using ReportTree.Server.Services;
using Xunit;

namespace ReportTree.Server.Tests.Security;

public class ExternalRoleMappingServiceTests
{
    [Fact]
    public void ResolveRoles_ReturnsDefaultRole_WhenRoleSyncDisabled()
    {
        var service = new ExternalRoleMappingService();
        var provider = new ExternalAuthProvider
        {
            RoleSyncEnabled = false,
            DefaultRole = "Editor"
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>()));
        var roles = service.ResolveRoles(provider, principal);

        Assert.Single(roles);
        Assert.Equal("Editor", roles[0]);
    }

    [Fact]
    public void ResolveRoles_MapsClaimRoles_WhenConfigured()
    {
        var service = new ExternalRoleMappingService();
        var provider = new ExternalAuthProvider
        {
            RoleSyncEnabled = true,
            RoleClaimType = "roles",
            DefaultRole = "Viewer",
            RoleMappings = new List<ExternalRoleMapping>
            {
                new() { ExternalRole = "pbi-admins", InternalRole = "Admin" },
                new() { ExternalRole = "pbi-editors", InternalRole = "Editor" }
            }
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("roles", "pbi-editors")
        }));

        var roles = service.ResolveRoles(provider, principal);

        Assert.Single(roles);
        Assert.Equal("Editor", roles[0]);
    }

    [Fact]
    public void ResolveRoles_FallsBackToDefault_WhenNoMappedExternalRolesFound()
    {
        var service = new ExternalRoleMappingService();
        var provider = new ExternalAuthProvider
        {
            RoleSyncEnabled = true,
            RoleClaimType = "roles",
            DefaultRole = "Viewer",
            RoleMappings = new List<ExternalRoleMapping>
            {
                new() { ExternalRole = "pbi-admins", InternalRole = "Admin" }
            }
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("roles", "unmapped-role")
        }));

        var roles = service.ResolveRoles(provider, principal);

        Assert.Single(roles);
        Assert.Equal("Viewer", roles[0]);
    }
}