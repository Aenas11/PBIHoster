namespace ReportTree.Server.Models;

public class PageVersion
{
    public int Id { get; set; }
    public int PageId { get; set; }
    public string Layout { get; set; } = "[]";
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string ChangeDescription { get; set; } = string.Empty;
}
