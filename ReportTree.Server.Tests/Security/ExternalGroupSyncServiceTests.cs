using System.Security.Claims;
using ReportTree.Server.Models;
using ReportTree.Server.Persistance;
using ReportTree.Server.Services;
using Xunit;

namespace ReportTree.Server.Tests.Security;

public class ExternalGroupSyncServiceTests
{
    [Fact]
    public async Task SyncMemberships_AddsMappedGroupMembership_WhenClaimPresent()
    {
        var groupRepo = new InMemoryGroupRepository(new List<Group>
        {
            new() { Id = 1, Name = "Sales", Members = new List<string>() }
        });

        var service = new ExternalGroupSyncService(groupRepo);
        var provider = new ExternalAuthProvider
        {
            Id = "entra",
            GroupSyncEnabled = true,
            GroupClaimType = "groups",
            GroupMappings = new List<ExternalGroupMapping>
            {
                new() { ExternalGroup = "entra-sales", InternalGroup = "Sales" }
            }
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("groups", "entra-sales")
        }));

        await service.SyncMembershipsAsync("oidc_entra_123", provider, principal);

        var groups = (await groupRepo.GetAllAsync()).ToList();
        Assert.Contains("oidc_entra_123", groups.Single(g => g.Name == "Sales").Members);
    }

    [Fact]
    public async Task SyncMemberships_DoesNotRemoveManagedGroupMembership_WhenRemoveDisabled()
    {
        var groupRepo = new InMemoryGroupRepository(new List<Group>
        {
            new() { Id = 1, Name = "Sales", Members = new List<string> { "oidc_entra_123" } }
        });

        var service = new ExternalGroupSyncService(groupRepo);
        var provider = new ExternalAuthProvider
        {
            Id = "entra",
            GroupSyncEnabled = true,
            GroupClaimType = "groups",
            RemoveUnmappedGroupMemberships = false,
            GroupMappings = new List<ExternalGroupMapping>
            {
                new() { ExternalGroup = "entra-sales", InternalGroup = "Sales" }
            }
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>()));

        await service.SyncMembershipsAsync("oidc_entra_123", provider, principal);

        var groups = (await groupRepo.GetAllAsync()).ToList();
        Assert.Contains("oidc_entra_123", groups.Single(g => g.Name == "Sales").Members);
    }

    [Fact]
    public async Task SyncMemberships_RemovesManagedGroupMembership_WhenRemoveEnabledAndClaimMissing()
    {
        var groupRepo = new InMemoryGroupRepository(new List<Group>
        {
            new() { Id = 1, Name = "Sales", Members = new List<string> { "oidc_entra_123" } }
        });

        var service = new ExternalGroupSyncService(groupRepo);
        var provider = new ExternalAuthProvider
        {
            Id = "entra",
            GroupSyncEnabled = true,
            GroupClaimType = "groups",
            RemoveUnmappedGroupMemberships = true,
            GroupMappings = new List<ExternalGroupMapping>
            {
                new() { ExternalGroup = "entra-sales", InternalGroup = "Sales" }
            }
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("groups", "different-group")
        }));

        await service.SyncMembershipsAsync("oidc_entra_123", provider, principal);

        var groups = (await groupRepo.GetAllAsync()).ToList();
        Assert.DoesNotContain("oidc_entra_123", groups.Single(g => g.Name == "Sales").Members);
    }

    private sealed class InMemoryGroupRepository : IGroupRepository
    {
        private readonly List<Group> _groups;

        public InMemoryGroupRepository(List<Group> groups)
        {
            _groups = groups;
        }

        public Task<IEnumerable<Group>> GetAllAsync() => Task.FromResult<IEnumerable<Group>>(_groups);

        public Task<IEnumerable<Group>> SearchAsync(string term) =>
            Task.FromResult<IEnumerable<Group>>(_groups.Where(g => g.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));

        public Task<int> CreateAsync(Group group)
        {
            group.Id = _groups.Count == 0 ? 1 : _groups.Max(g => g.Id) + 1;
            _groups.Add(group);
            return Task.FromResult(group.Id);
        }

        public Task DeleteAsync(int id)
        {
            _groups.RemoveAll(g => g.Id == id);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Group group)
        {
            var index = _groups.FindIndex(g => g.Id == group.Id);
            if (index >= 0)
            {
                _groups[index] = group;
            }

            return Task.CompletedTask;
        }
    }
}