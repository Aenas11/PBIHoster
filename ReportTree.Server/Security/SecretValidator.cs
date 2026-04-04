using ReportTree.Server.Models;
using System.Text;

namespace ReportTree.Server.Security;

public static class SecretValidator
{
    private const int MinimumKeyLength = 32;

    public static void Validate(IConfiguration configuration)
    {
        var missingSecrets = new List<string>();

        var jwtKey = configuration["Jwt:Key"];
        var environment = configuration["ASPNETCORE_ENVIRONMENT"];

        var isDevelopment = string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);


        if (!isDevelopment)
        {
            if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < MinimumKeyLength || jwtKey.Contains("change", StringComparison.OrdinalIgnoreCase))
            {
            missingSecrets.Add("Jwt:Key");
            }
        }

        ValidatePowerBiConfiguration(configuration, missingSecrets);
        ValidateExternalAuthConfiguration(configuration, missingSecrets, isDevelopment);

        if (missingSecrets.Count > 0)
        {
            throw new InvalidOperationException($"Missing or insecure secrets: {string.Join(", ", missingSecrets)}");
        }
    }

    private static void ValidatePowerBiConfiguration(IConfiguration configuration, List<string> missingSecrets)
    {
        var tenantId = configuration["PowerBI:TenantId"];
        var clientId = configuration["PowerBI:ClientId"];
        var authType = configuration["PowerBI:AuthType"] ?? AuthenticationType.ClientSecret.ToString();

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            missingSecrets.Add("PowerBI:TenantId");
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            missingSecrets.Add("PowerBI:ClientId");
        }

        if (!Enum.TryParse(authType, out AuthenticationType parsedAuthType))
        {
            throw new InvalidOperationException("PowerBI:AuthType must be either ClientSecret or Certificate.");
        }

        if (parsedAuthType == AuthenticationType.ClientSecret)
        {
            if (string.IsNullOrWhiteSpace(configuration["PowerBI:ClientSecret"]))
            {
                missingSecrets.Add("PowerBI:ClientSecret");
            }
        }
        else
        {
            var certificateThumbprint = configuration["PowerBI:CertificateThumbprint"];
            var certificatePath = configuration["PowerBI:CertificatePath"];

            if (string.IsNullOrWhiteSpace(certificateThumbprint) && string.IsNullOrWhiteSpace(certificatePath))
            {
                missingSecrets.Add("PowerBI:CertificateThumbprint or PowerBI:CertificatePath");
            }
        }
    }

    private static void ValidateExternalAuthConfiguration(IConfiguration configuration, List<string> missingSecrets, bool isDevelopment)
    {
        var externalAuthSection = configuration.GetSection("Security:ExternalAuth");
        var enabled = externalAuthSection.GetValue<bool>("Enabled");

        if (!enabled)
        {
            return;
        }

        var providers = externalAuthSection.GetSection("Providers").Get<List<ExternalAuthProvider>>() ?? new List<ExternalAuthProvider>();

        if (providers.Count == 0)
        {
            missingSecrets.Add("Security:ExternalAuth:Providers");
            return;
        }

        foreach (var provider in providers.Where(p => p.Enabled))
        {
            if (string.IsNullOrWhiteSpace(provider.Id))
            {
                missingSecrets.Add("Security:ExternalAuth:Providers[*]:Id");
            }

            if (string.IsNullOrWhiteSpace(provider.Authority))
            {
                missingSecrets.Add($"Security:ExternalAuth:Providers:{provider.Id}:Authority");
            }

            if (string.IsNullOrWhiteSpace(provider.ClientId))
            {
                missingSecrets.Add($"Security:ExternalAuth:Providers:{provider.Id}:ClientId");
            }

            if (!isDevelopment && string.IsNullOrWhiteSpace(provider.ClientSecret))
            {
                missingSecrets.Add($"Security:ExternalAuth:Providers:{provider.Id}:ClientSecret");
            }
        }
    }
}
