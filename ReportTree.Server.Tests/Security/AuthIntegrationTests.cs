using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ReportTree.Server.Tests.Security;

public class AuthIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RegisterLoginRefresh_WorksEndToEnd()
    {
        var username = $"user_{Guid.NewGuid():N}";
        const string password = "Password123!";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            password,
            roles = new[] { "Viewer" }
        });

        Assert.True(registerResponse.IsSuccessStatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password
        });

        Assert.True(loginResponse.IsSuccessStatusCode);
        var token = await ExtractTokenAsync(loginResponse);
        Assert.False(string.IsNullOrWhiteSpace(token));

        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) }
        };

        var refreshResponse = await _client.SendAsync(refreshRequest);
        Assert.True(refreshResponse.IsSuccessStatusCode);

        var refreshedToken = await ExtractTokenAsync(refreshResponse);
        Assert.False(string.IsNullOrWhiteSpace(refreshedToken));
    }

    [Fact]
    public async Task InvalidLogin_AttemptsEventuallyLockAccount()
    {
        var username = $"locked_{Guid.NewGuid():N}";
        const string password = "Password123!";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            password,
            roles = new[] { "Viewer" }
        });
        Assert.True(registerResponse.IsSuccessStatusCode);

        string lastErrorBody = string.Empty;
        for (var attempt = 0; attempt < 6; attempt++)
        {
            var badLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                username,
                password = "WrongPassword123!"
            });

            Assert.False(badLoginResponse.IsSuccessStatusCode);
            lastErrorBody = await badLoginResponse.Content.ReadAsStringAsync();
        }

        Assert.True(
            lastErrorBody.Contains("locked", StringComparison.OrdinalIgnoreCase)
            || lastErrorBody.Contains("quota", StringComparison.OrdinalIgnoreCase),
            $"Expected lockout or throttling response, got: {lastErrorBody}");
    }

    private static async Task<string> ExtractTokenAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("token", out var tokenProp))
        {
            return tokenProp.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}
