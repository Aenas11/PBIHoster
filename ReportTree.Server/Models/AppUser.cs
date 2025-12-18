namespace ReportTree.Server.Models
{
    public class AppUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public List<string> Groups { get; set; } = new();
    }
}
