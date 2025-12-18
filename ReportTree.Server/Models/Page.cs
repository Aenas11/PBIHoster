namespace ReportTree.Server.Models
{
    public class Page
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public int Order { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
