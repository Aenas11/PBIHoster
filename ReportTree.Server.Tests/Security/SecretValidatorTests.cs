using Microsoft.Extensions.Configuration;
using ReportTree.Server.Security;
using Xunit;

namespace ReportTree.Server.Tests.Security;

public class SecretValidatorTests
{
    [Fact]
    public void ThrowsWhenSecretsMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "",
                ["PowerBI:TenantId"] = "tenant",
                ["PowerBI:ClientId"] = "",
                ["PowerBI:AuthType"] = "ClientSecret"
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() => SecretValidator.Validate(configuration));
    }

    [Fact]
    public void AllowsCertificateBasedAuthWhenConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-value-that-is-long-enough-123",
                ["PowerBI:TenantId"] = "tenant",
                ["PowerBI:ClientId"] = "client",
                ["PowerBI:AuthType"] = "Certificate",
                ["PowerBI:CertificateThumbprint"] = "thumbprint"
            })
            .Build();

        var exception = Record.Exception(() => SecretValidator.Validate(configuration));

        Assert.Null(exception);
    }
}
