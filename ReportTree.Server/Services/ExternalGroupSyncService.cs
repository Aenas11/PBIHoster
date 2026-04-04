using System.Security.Claims;
using ReportTree.Server.Models;
using ReportTree.Server.Persistance;

namespace ReportTree.Server.Services;

public class ExternalGroupSyncService
{
    private readonly IGroupRepository _groupRepository;

    public ExternalGroupSyncService(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task SyncMembershipsAsync(string username, ExternalAuthProvider provider, ClaimsPrincipal principal)
    {
        if (!provider.GroupSyncEnabled)
        {
            return;
        }

        var externalGroups = principal
            .Claims
            .Where(c => string.Equals(c.Type, provider.GroupClaimType, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value?.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (externalGroups.Count == 0)
        {
            return;
        }

        var mappedInternalGroups = provider.GroupMappings
            .Where(m => externalGroups.Contains(m.ExternalGroup))
            .Select(m => m.InternalGroup)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (mappedInternalGroups.Count == 0 && !provider.RemoveUnmappedGroupMemberships)
        {
            return;
        }

        var managedGroups = provider.GroupMappings
            .Select(m => m.InternalGroup)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingGroups = (await _groupRepository.GetAllAsync()).ToList();
        foreach (var group in existingGroups)
        {
            var isManaged = managedGroups.Contains(group.Name);
            if (!isManaged)
            {
                continue;
            }

            var hasMember = group.Members.Contains(username, StringComparer.OrdinalIgnoreCase);
            var shouldBeMember = mappedInternalGroups.Contains(group.Name);

            if (shouldBeMember && !hasMember)
            {
                group.Members.Add(username);
                await _groupRepository.UpdateAsync(group);
                continue;
            }

            if (!shouldBeMember && hasMember && provider.RemoveUnmappedGroupMemberships)
            {
                group.Members = group.Members
                    .Where(m => !string.Equals(m, username, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                await _groupRepository.UpdateAsync(group);
            }
        }
    }
}