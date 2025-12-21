using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using ReportTree.Server;

namespace ReportTree.Server.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public TestWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Key", "test-signing-key-value-that-is-long-enough-123");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "ReportTree-Test");
        Environment.SetEnvironmentVariable("PowerBI__TenantId", "test-tenant");
        Environment.SetEnvironmentVariable("PowerBI__ClientId", "test-client");
        Environment.SetEnvironmentVariable("PowerBI__ClientSecret", "test-secret");
        Environment.SetEnvironmentVariable("PowerBI__AuthType", "ClientSecret");
        Environment.SetEnvironmentVariable("Security__CorsPolicy__AllowedOrigins__0", "https://reports.example.com");
        Environment.SetEnvironmentVariable("Security__CorsPolicy__AllowCredentials", "true");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Security:CorsPolicy:AllowedOrigins:0"] = "https://reports.example.com",
                ["Security:CorsPolicy:AllowCredentials"] = "true"
            };

            configBuilder.AddInMemoryCollection(settings);
        });
    }
}
