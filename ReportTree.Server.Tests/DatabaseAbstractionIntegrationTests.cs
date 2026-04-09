using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using ReportTree.Server;
using Xunit;

namespace ReportTree.Server.Tests;

public class DatabaseAbstractionIntegrationTests : IClassFixture<SqliteWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DatabaseAbstractionIntegrationTests(SqliteWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SqliteProvider_RegisterLoginAndPageCrudFlow_Works()
    {
        var adminToken = await RegisterAndLoginAsync($"admin_{Guid.NewGuid():N}", "Password123!", new[] { "Admin" });

        using var createRequest = BuildAuthorizedRequest(HttpMethod.Post, "/api/pages", adminToken, new
        {
            title = "Relational Page",
            icon = "Document",
            parentId = (int?)null,
            order = 0,
            isPublic = true,
            sensitivityLabel = "Internal",
            layout = "[]",
            allowedUsers = Array.Empty<string>(),
            allowedGroups = Array.Empty<string>(),
            isDemo = false
        });

        var createResponse = await _client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var payload = await createResponse.Content.ReadAsStringAsync();
        using var createJson = JsonDocument.Parse(payload);
        var pageId = createJson.RootElement.GetProperty("id").GetInt32();

        var getResponse = await _client.GetAsync($"/api/pages/{pageId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task SqliteProvider_AuditExportCsvEndpoint_ReturnsFile()
    {
        var adminToken = await RegisterAndLoginAsync($"admin_{Guid.NewGuid():N}", "Password123!", new[] { "Admin" });
        await UpsertSettingAsync(adminToken, "App.HomePageId", "1", "Application", "Set home page");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/audit/export?format=csv&actionType=UPDATE");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"UPDATE\"", csv, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SqliteProvider_AuditQueryRejectsInvalidDateRange()
    {
        var adminToken = await RegisterAndLoginAsync($"admin_{Guid.NewGuid():N}", "Password123!", new[] { "Admin" });
        var fromUtc = DateTime.UtcNow;
        var toUtc = fromUtc.AddMinutes(-10);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/audit?fromUtc={Uri.EscapeDataString(fromUtc.ToString("O"))}&toUtc={Uri.EscapeDataString(toUtc.ToString("O"))}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<string> RegisterAndLoginAsync(string username, string password, string[] roles)
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            password,
            roles
        });
        Assert.True(registerResponse.IsSuccessStatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password
        });
        Assert.True(loginResponse.IsSuccessStatusCode);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(loginBody);
        return doc.RootElement.GetProperty("token").GetString() ?? string.Empty;
    }

    private async Task UpsertSettingAsync(string token, string key, string value, string category, string description)
    {
        using var request = BuildAuthorizedRequest(HttpMethod.Put, "/api/settings", token, new
        {
            key,
            value,
            category,
            description
        });

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static HttpRequestMessage BuildAuthorizedRequest(HttpMethod method, string url, string token, object body)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}

public class SqliteWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath;
    private readonly string _assetsPath;

    public SqliteWebApplicationFactory()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"reporttree-relational-tests-{Guid.NewGuid():N}.db");
        _assetsPath = Path.Combine(Path.GetTempPath(), $"reporttree-relational-assets-{Guid.NewGuid():N}");

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
                ["Database:Provider"] = "Sqlite",
                ["Database:ConnectionString"] = $"Data Source={_dbPath}",
                ["Database:BrandingAssetsPath"] = _assetsPath,
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

        if (!disposing)
        {
            return;
        }

        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }

            if (Directory.Exists(_assetsPath))
            {
                Directory.Delete(_assetsPath, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup for temporary test files.
        }
    }
}