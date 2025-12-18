namespace ReportTree.Server.Models
{
    public class AppSetting
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsEncrypted { get; set; }
        public string Category { get; set; } = "General";
        public string Description { get; set; } = string.Empty;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public string ModifiedBy { get; set; } = string.Empty;
    }
}
