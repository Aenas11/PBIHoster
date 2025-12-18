namespace ReportTree.Server.Models
{
    public class Page
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public int Order { get; set; }
        public bool IsPublic { get; set; }
        public string Layout { get; set; } = "[]";
        public List<string> AllowedUsers { get; set; } = new();
        public List<string> AllowedGroups { get; set; } = new();
        
        // Optional: If this page uses PowerBIWorkspaceComponent, store workspace ID here for easy reference
        // This is a convenience field to avoid parsing Layout JSON for workspace queries
        public Guid? PowerBIWorkspaceId { get; set; }
    }
}
