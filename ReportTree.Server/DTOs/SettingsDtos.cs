namespace ReportTree.Server.DTOs;

public record SettingDto(string Key, string Value, string Category, string Description, bool IsEncrypted);
public record SettingUpdateDto(string Key, string Value, string Category, string Description);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record UserProfileDto(string Username, string Email, List<string> Roles, List<string> Groups);
public record UpdateProfileRequest(string Email);
