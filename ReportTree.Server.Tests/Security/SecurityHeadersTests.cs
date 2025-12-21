using Microsoft.AspNetCore.Mvc.Testing;
using ReportTree.Server;
using Xunit;

namespace ReportTree.Server.Tests.Security;

public class SecurityHeadersTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityHeadersTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AddsCspHeadersWithPowerBiSources()
    {
        var response = await _client.GetAsync("/healthz");

        Assert.True(response.Headers.TryGetValues("Content-Security-Policy", out var cspValues));
        var csp = string.Join(' ', cspValues);

        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("frame-src 'self' https://app.powerbi.com https://*.powerbi.com https://reports.example.com", csp);
        Assert.Contains("script-src 'self' https://js.powerbi.com", csp);
    }

    [Fact]
    public async Task AllowsCorsForConfiguredOrigin()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/healthz");
        request.Headers.Add("Origin", "https://reports.example.com");

        var response = await _client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Contains("https://reports.example.com", origins);
    }
}
