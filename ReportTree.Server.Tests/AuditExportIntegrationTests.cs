using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ReportTree.Server.Tests;

public class AuditExportIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuditExportIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AdminCanExportFilteredAuditLogsAsCsv()
    {
        var adminUsername = $"admin_{Guid.NewGuid():N}";
        var editorUsername = $"editor_{Guid.NewGuid():N}";
        var adminToken = await RegisterAndLoginAsync(adminUsername, "Password123!", new[] { "Admin" });
        var editorToken = await RegisterAndLoginAsync(editorUsername, "Password123!", new[] { "Editor" });

        var fromUtc = DateTime.UtcNow.AddMinutes(-1);

        await CreatePageAsync(editorToken, "Exported Page");
        await UpsertSettingAsync(adminToken, "App.HomePageId", "1", "Application", "Set home page");

        var toUtc = DateTime.UtcNow.AddMinutes(1);
        var url = $"/api/audit/export?format=csv&username={Uri.EscapeDataString(editorUsername)}&actionType=SET_SENSITIVITY_LABEL&fromUtc={Uri.EscapeDataString(fromUtc.ToString("O"))}&toUtc={Uri.EscapeDataString(toUtc.ToString("O"))}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("audit-export-", response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName ?? string.Empty);

        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains(editorUsername, csv, StringComparison.Ordinal);
        Assert.Contains("SET_SENSITIVITY_LABEL", csv, StringComparison.Ordinal);
        Assert.DoesNotContain(adminUsername, csv, StringComparison.Ordinal);
        Assert.DoesNotContain("\"UPDATE\"", csv, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminCanExportAuditLogsAsPdf()
    {
        var adminToken = await RegisterAndLoginAsync($"admin_{Guid.NewGuid():N}", "Password123!", new[] { "Admin" });
        await UpsertSettingAsync(adminToken, "Branding.AppName", "PBIHoster", "Branding", "Set app name");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/audit/export?format=pdf&actionType=UPDATE");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var header = Encoding.ASCII.GetString(bytes.Take(8).ToArray());
        Assert.StartsWith("%PDF-1.", header, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuditExportRejectsInvalidDateRange()
    {
        var adminToken = await RegisterAndLoginAsync($"admin_{Guid.NewGuid():N}", "Password123!", new[] { "Admin" });
        var fromUtc = DateTime.UtcNow;
        var toUtc = fromUtc.AddMinutes(-5);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/audit/export?format=csv&fromUtc={Uri.EscapeDataString(fromUtc.ToString("O"))}&toUtc={Uri.EscapeDataString(toUtc.ToString("O"))}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task CreatePageAsync(string token, string title)
    {
        using var request = BuildAuthorizedRequest(HttpMethod.Post, "/api/pages", token, new
        {
            title,
            icon = "Document",
            parentId = (int?)null,
            order = 0,
            isPublic = true,
            sensitivityLabel = "Confidential",
            layout = "[]",
            allowedUsers = Array.Empty<string>(),
            allowedGroups = Array.Empty<string>(),
            isDemo = false
        });

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
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