using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using ReportTree.Server;

namespace ReportTree.Server.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath;

    public TestWebApplicationFactory()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"reporttree-tests-{Guid.NewGuid():N}.db");

        Environment.SetEnvironmentVariable("Jwt__Key", "test-signing-key-value-that-is-long-enough-123");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "ReportTree-Test");
        Environment.SetEnvironmentVariable("PowerBI__TenantId", "test-tenant");
        Environment.SetEnvironmentVariable("PowerBI__ClientId", "test-client");
        Environment.SetEnvironmentVariable("PowerBI__ClientSecret", "test-secret");
        Environment.SetEnvironmentVariable("PowerBI__AuthType", "ClientSecret");
        Environment.SetEnvironmentVariable("Security__RateLimitPolicy__Enabled", "false");
        Environment.SetEnvironmentVariable("Security__CorsPolicy__AllowedOrigins__0", "https://reports.example.com");
        Environment.SetEnvironmentVariable("Security__CorsPolicy__AllowCredentials", "true");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["LiteDb:ConnectionString"] = $"Filename={_dbPath};Connection=shared",
                ["Security:RateLimitPolicy:Enabled"] = "false",
                ["Security:CorsPolicy:AllowedOrigins:0"] = "https://reports.example.com",
                ["Security:CorsPolicy:AllowCredentials"] = "true"
            };

            configBuilder.AddInMemoryCollection(settings);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            try
            {
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
            }
            catch
            {
                // Best effort cleanup for temporary DB files.
            }
        }
    }
}
