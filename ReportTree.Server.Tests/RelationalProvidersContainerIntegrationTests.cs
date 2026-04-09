using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using ReportTree.Server;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;
using Xunit;

namespace ReportTree.Server.Tests;

public class RelationalProvidersContainerIntegrationTests
{
    private static bool ShouldRunContainerTests()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable("RUN_RELATIONAL_CONTAINER_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task SqlServerProvider_RegisterAndLogin_Works_WhenContainerTestsEnabled()
    {
        Skip.IfNot(ShouldRunContainerTests(), "Set RUN_RELATIONAL_CONTAINER_TESTS=true to enable SQL Server container integration tests.");

        var sqlServer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithEnvironment("MSSQL_SA_PASSWORD", "PBIHoster_StrongPass_123!")
            .Build();

        await sqlServer.StartAsync();

        var assetsPath = Path.Combine(Path.GetTempPath(), $"reporttree-sqlserver-assets-{Guid.NewGuid():N}");
        await using var factory = new ProviderWebApplicationFactory(
            provider: "SqlServer",
            connectionString: sqlServer.GetConnectionString(),
            assetsPath: assetsPath);

        var client = factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, $"sqladmin_{Guid.NewGuid():N}", "Password123!", new[] { "Admin" });

        Assert.False(string.IsNullOrWhiteSpace(token));

        await sqlServer.DisposeAsync();
        CleanupDirectory(assetsPath);
    }

    [SkippableFact]
    public async Task PostgreSqlProvider_RegisterAndLogin_Works_WhenContainerTestsEnabled()
    {
        Skip.IfNot(ShouldRunContainerTests(), "Set RUN_RELATIONAL_CONTAINER_TESTS=true to enable PostgreSQL container integration tests.");

        var postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("reporttree_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await postgres.StartAsync();

        var assetsPath = Path.Combine(Path.GetTempPath(), $"reporttree-postgres-assets-{Guid.NewGuid():N}");
        await using var factory = new ProviderWebApplicationFactory(
            provider: "PostgreSql",
            connectionString: postgres.GetConnectionString(),
            assetsPath: assetsPath);

        var client = factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, $"pgadmin_{Guid.NewGuid():N}", "Password123!", new[] { "Admin" });

        Assert.False(string.IsNullOrWhiteSpace(token));

        await postgres.DisposeAsync();
        CleanupDirectory(assetsPath);
    }

    private static async Task<string> RegisterAndLoginAsync(HttpClient client, string username, string password, string[] roles)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            password,
            roles
        });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(loginBody);
        return doc.RootElement.GetProperty("token").GetString() ?? string.Empty;
    }

    private static void CleanupDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup for temporary files.
        }
    }

    private sealed class ProviderWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _provider;
        private readonly string _connectionString;
        private readonly string _assetsPath;

        public ProviderWebApplicationFactory(string provider, string connectionString, string assetsPath)
        {
            _provider = provider;
            _connectionString = connectionString;
            _assetsPath = assetsPath;

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
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                var settings = new Dictionary<string, string?>
                {
                    ["Database:Provider"] = _provider,
                    ["Database:ConnectionString"] = _connectionString,
                    ["Database:BrandingAssetsPath"] = _assetsPath,
                    ["Security:RateLimitPolicy:Enabled"] = "false",
                    ["Security:CorsPolicy:AllowedOrigins:0"] = "https://reports.example.com",
                    ["Security:CorsPolicy:AllowCredentials"] = "true"
                };

                configBuilder.AddInMemoryCollection(settings);
            });
        }
    }
}