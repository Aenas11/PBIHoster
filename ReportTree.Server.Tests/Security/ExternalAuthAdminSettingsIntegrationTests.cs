using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ReportTree.Server.Tests.Security;

public class ExternalAuthAdminSettingsIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ExternalAuthAdminSettingsIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ExternalAuthProviders_AdminCanReadSettings()
    {
        var adminToken = await RegisterAndLoginAsync($"admin_{Guid.NewGuid():N}", "Password123!", new[] { "Admin" });

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/settings/external-auth/providers");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task ExternalAuthProviders_AdminCanUpdateOverrides()
    {
        var adminToken = await RegisterAndLoginAsync($"admin_{Guid.NewGuid():N}", "Password123!", new[] { "Admin" });

        var providerId = await GetFirstProviderIdAsync(adminToken);
        Assert.False(string.IsNullOrWhiteSpace(providerId));

        var updateRequest = new HttpRequestMessage(HttpMethod.Put, "/api/settings/external-auth/providers")
        {
            Content = JsonContent.Create(new
            {
                providers = new[]
                {
                    new
                    {
                        providerId,
                        defaultRole = "Editor",
                        groupSyncEnabled = true,
                        groupClaimType = "groups",
                        removeUnmappedGroupMemberships = true,
                        groupMappings = new[]
                        {
                            new { externalGroup = "entra-sales", internalGroup = "Sales" }
                        },
                        roleSyncEnabled = true,
                        roleClaimType = "roles",
                        roleMappings = new[]
                        {
                            new { externalRole = "entra-admins", internalRole = "Admin" }
                        }
                    }
                }
            })
        };
        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var updateResponse = await _client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var verifyRequest = new HttpRequestMessage(HttpMethod.Get, "/api/settings/external-auth/providers");
        verifyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var verifyResponse = await _client.SendAsync(verifyRequest);
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        var verifyBody = await verifyResponse.Content.ReadAsStringAsync();
        using var verifyDoc = JsonDocument.Parse(verifyBody);

        var provider = verifyDoc.RootElement
            .EnumerateArray()
            .FirstOrDefault(p => string.Equals(p.GetProperty("providerId").GetString(), providerId, StringComparison.OrdinalIgnoreCase));

        Assert.Equal("Editor", provider.GetProperty("defaultRole").GetString());
        Assert.True(provider.GetProperty("groupSyncEnabled").GetBoolean());
        Assert.True(provider.GetProperty("removeUnmappedGroupMemberships").GetBoolean());
        Assert.True(provider.GetProperty("roleSyncEnabled").GetBoolean());

        var roleMappings = provider.GetProperty("roleMappings").EnumerateArray().ToList();
        Assert.Contains(roleMappings, mapping =>
            string.Equals(mapping.GetProperty("externalRole").GetString(), "entra-admins", StringComparison.OrdinalIgnoreCase)
            && string.Equals(mapping.GetProperty("internalRole").GetString(), "Admin", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> GetFirstProviderIdAsync(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/settings/external-auth/providers");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var first = doc.RootElement.EnumerateArray().FirstOrDefault();
        return first.ValueKind == JsonValueKind.Object
            ? first.GetProperty("providerId").GetString() ?? string.Empty
            : string.Empty;
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