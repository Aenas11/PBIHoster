namespace ReportTree.Server.DTOs
{
    public class WorkspaceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ReportDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EmbedUrl { get; set; } = string.Empty;
        public string DatasetId { get; set; } = string.Empty;
    }

    public class DashboardDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EmbedUrl { get; set; } = string.Empty;
    }

    public class EmbedTokenRequestDto
    {
        public Guid WorkspaceId { get; set; }
        public Guid ResourceId { get; set; }
        public string ResourceType { get; set; } = "Report"; // "Report" or "Dashboard"
        public int? PageId { get; set; } // Optional, for authorization check
        public bool EnableRLS { get; set; } // Enable Row Level Security
        public List<string>? RLSRoles { get; set; } // RLS roles to apply
    }

    public class EmbedTokenResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string EmbedUrl { get; set; } = string.Empty;
        public string TokenId { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }

    public class PowerBIResourceDto
    {
        public string Type { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class RLSIdentityDto
    {
        public string Username { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public List<string> Datasets { get; set; } = new();
    }

}
