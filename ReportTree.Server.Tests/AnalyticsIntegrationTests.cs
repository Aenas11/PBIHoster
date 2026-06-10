using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ReportTree.Server.Tests;

public class AnalyticsIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AnalyticsIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SummaryIncludesDailySeriesAndDeviceTypes()
    {
        var adminToken = await RegisterAndLoginAsync($"admin_{Guid.NewGuid():N}", "Password123!", new[] { "Admin" });

        // Ingest a couple of events
        var ingestResponse = await _client.PostAsJsonAsync("/api/analytics/events", new
        {
            events = new[]
            {
                new { eventType = "page_view", path = "/pages/1", deviceType = "desktop" },
                new { eventType = "report_view", path = "/pages/2", deviceType = "mobile" }
            }
        });
        Assert.True(ingestResponse.IsSuccessStatusCode);

        using var summaryRequest = new HttpRequestMessage(HttpMethod.Get, "/api/analytics/summary?days=7");
        summaryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var summaryResponse = await _client.SendAsync(summaryRequest);

        Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);

        var body = await summaryResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("dailySeries", out var dailySeries),
            "Response should contain dailySeries");
        Assert.True(dailySeries.GetArrayLength() > 0,
            "dailySeries should have at least one entry");

        Assert.True(root.TryGetProperty("deviceTypes", out var deviceTypes),
            "Response should contain deviceTypes");
        Assert.True(deviceTypes.GetArrayLength() > 0,
            "deviceTypes should have at least one entry");

        // Verify daily series entry shape
        var firstDay = dailySeries[0];
        Assert.True(firstDay.TryGetProperty("date", out _), "Daily entry should have date");
        Assert.True(firstDay.TryGetProperty("totalEvents", out _), "Daily entry should have totalEvents");
        Assert.True(firstDay.TryGetProperty("pageViews", out _), "Daily entry should have pageViews");
        Assert.True(firstDay.TryGetProperty("reportViews", out _), "Daily entry should have reportViews");
        Assert.True(firstDay.TryGetProperty("uniqueUsers", out _), "Daily entry should have uniqueUsers");
    }

    [Fact]
    public async Task ExportReturnsCsvWithHeaders()
    {
        var adminToken = await RegisterAndLoginAsync($"admin_{Guid.NewGuid():N}", "Password123!", new[] { "Admin" });

        // Ingest an event
        await _client.PostAsJsonAsync("/api/analytics/events", new
        {
            events = new[]
            {
                new { eventType = "page_view", path = "/pages/dashboard", deviceType = "desktop" }
            }
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/analytics/export?days=7");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("analytics-export-", response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? string.Empty);

        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("Timestamp,EventType,Username,Path,DeviceType", csv, StringComparison.Ordinal);
        Assert.Contains("page_view", csv, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportRequiresAdminRole()
    {
        var viewerToken = await RegisterAndLoginAsync($"viewer_{Guid.NewGuid():N}", "Password123!", new[] { "Viewer" });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/analytics/export?days=7");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", viewerToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task LoginTracksUserLoginEvent()
    {
        var adminUsername = $"admin_{Guid.NewGuid():N}";
        var adminToken = await RegisterAndLoginAsync(adminUsername, "Password123!", new[] { "Admin" });

        using var summaryRequest = new HttpRequestMessage(HttpMethod.Get, "/api/analytics/summary?days=1");
        summaryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var summaryResponse = await _client.SendAsync(summaryRequest);

        Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);
        var body = await summaryResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var eventTypes = doc.RootElement.GetProperty("eventTypes");
        var loginEntry = eventTypes.EnumerateArray()
            .FirstOrDefault(e => e.TryGetProperty("eventType", out var et) &&
                                 et.GetString() == "user_login");

        Assert.True(loginEntry.ValueKind != JsonValueKind.Undefined,
            "A user_login event should be recorded after login");
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
}
