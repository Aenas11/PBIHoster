using ReportTree.Server.Models;

namespace ReportTree.Server.DTOs;

public record CreateThemeDto(
    string Name,
    Dictionary<string, string> Tokens,
    string? OrganizationId
);

public record UpdateThemeDto(
    string Name,
    Dictionary<string, string> Tokens
);

public record ThemeDto(
    string Id,
    string Name,
    Dictionary<string, string> Tokens,
    bool IsCustom,
    string? OrganizationId,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public static class ThemeExtensions
{
    public static ThemeDto ToDto(this CustomTheme theme)
    {
        return new ThemeDto(
            theme.Id,
            theme.Name,
            theme.Tokens,
            theme.IsCustom,
            theme.OrganizationId,
            theme.CreatedBy,
            theme.CreatedAt,
            theme.UpdatedAt
        );
    }
}
