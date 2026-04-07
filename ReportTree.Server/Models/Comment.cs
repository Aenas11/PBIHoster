namespace ReportTree.Server.Models;

public class Comment
{
    public int Id { get; set; }
    public int PageId { get; set; }
    public int? ParentId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Mentions { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}