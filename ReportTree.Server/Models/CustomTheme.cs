namespace ReportTree.Server.Models;

public class CustomTheme
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Tokens { get; set; } = new();
    public bool IsCustom { get; set; } = true;
    public string? OrganizationId { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
