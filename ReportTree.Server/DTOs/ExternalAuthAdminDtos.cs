using System.ComponentModel.DataAnnotations;

namespace ReportTree.Server.DTOs;

public record ExternalAuthAdminConfigResponse(
    string ProviderId,
    string DisplayName,
    bool Enabled,
    string DefaultRole,
    bool GroupSyncEnabled,
    string GroupClaimType,
    bool RemoveUnmappedGroupMemberships,
    List<ExternalGroupMappingDto> GroupMappings,
    bool RoleSyncEnabled,
    string RoleClaimType,
    List<ExternalRoleMappingDto> RoleMappings
);

public record ExternalGroupMappingDto(
    [Required] string ExternalGroup,
    [Required] string InternalGroup
);

public record ExternalRoleMappingDto(
    [Required] string ExternalRole,
    [Required] string InternalRole
);

public record ExternalAuthAdminConfigUpdateRequest(
    [Required] List<ExternalAuthAdminProviderUpdateDto> Providers
);

public record ExternalAuthAdminProviderUpdateDto(
    [Required] string ProviderId,
    [Required] string DefaultRole,
    bool GroupSyncEnabled,
    string GroupClaimType,
    bool RemoveUnmappedGroupMemberships,
    List<ExternalGroupMappingDto> GroupMappings,
    bool RoleSyncEnabled,
    string RoleClaimType,
    List<ExternalRoleMappingDto> RoleMappings
);