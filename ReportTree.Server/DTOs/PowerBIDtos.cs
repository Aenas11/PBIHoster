namespace ReportTree.Server.DTOs
{
    using System.Text.Json.Serialization;

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

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DiagnosticStatus
    {
        Success,
        Warning,
        Error
    }

    public class PowerBIDiagnosticCheckDto
    {
        public string Name { get; set; } = string.Empty;
        public DiagnosticStatus Status { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string? Resolution { get; set; }
        public string? DocsUrl { get; set; }
    }

    public class PowerBIDiagnosticResultDto
    {
        public bool Success { get; set; }
        public Guid? WorkspaceId { get; set; }
        public Guid? ReportId { get; set; }
        public string? AzurePortalLink { get; set; }
        public IEnumerable<WorkspaceDto> Workspaces { get; set; } = Enumerable.Empty<WorkspaceDto>();
        public IEnumerable<ReportDto> Reports { get; set; } = Enumerable.Empty<ReportDto>();
        public IEnumerable<PowerBIDiagnosticCheckDto> Checks { get; set; } = Enumerable.Empty<PowerBIDiagnosticCheckDto>();
    }

}
