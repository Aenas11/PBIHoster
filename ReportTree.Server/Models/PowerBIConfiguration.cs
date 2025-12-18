namespace ReportTree.Server.Models
{
    public class PowerBIConfiguration
    {
        public string AuthorityUrl { get; set; } = "https://login.microsoftonline.com/{0}/";
        public string ResourceUrl { get; set; } = "https://analysis.windows.net/powerbi/api";
        public string ApiUrl { get; set; } = "https://api.powerbi.com";
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string CertificateThumbprint { get; set; } = string.Empty;
        public string CertificatePath { get; set; } = string.Empty;
        public AuthenticationType AuthType { get; set; } = AuthenticationType.ClientSecret;
    }

    public enum AuthenticationType
    {
        ClientSecret,
        Certificate
    }
}
