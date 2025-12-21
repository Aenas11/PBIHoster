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
}
