using System.ComponentModel.DataAnnotations;

namespace ReportTree.Server.Models;

public class ExternalAuthOptions
{
    public bool Enabled { get; set; }
    public List<ExternalAuthProvider> Providers { get; set; } = new();
}

public class ExternalAuthProvider
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string DisplayName { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    [Required]
    public string Authority { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public bool RequireHttpsMetadata { get; set; } = true;

    public string CallbackPath { get; set; } = string.Empty;

    public List<string> Scopes { get; set; } = new() { "openid", "profile", "email" };

    public string DefaultRole { get; set; } = "Viewer";

    public string Scheme => $"oidc:{Id}";

    public string GetCallbackPathOrDefault() =>
        string.IsNullOrWhiteSpace(CallbackPath) ? $"/signin-oidc-{Id}" : CallbackPath;
}