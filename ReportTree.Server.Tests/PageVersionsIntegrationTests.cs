using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ReportTree.Server.Tests;

public class PageVersionsIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PageVersionsIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SaveLayout_CreatesVersions_AndRollbackRestoresLayout()
    {
        var editorToken = await RegisterAndLoginAsync($"editor_{Guid.NewGuid():N}", "Password123!", new[] { "Editor" });
        var pageId = await CreatePageAsync(editorToken, "Versioned Page", isPublic: true);

        var layoutV1 = new[]
        {
            new
            {
                i = "panel-0",
                x = 0,
                y = 0,
                w = 6,
                h = 6,
                minW = 2,
                minH = 2,
                componentType = "favorites",
                componentConfig = new { showFavorites = true },
                metadata = new { title = "Favorites", description = "", createdAt = DateTime.UtcNow.ToString("o"), updatedAt = DateTime.UtcNow.ToString("o") }
            }
        };

        var layoutV2 = new[]
        {
            new
            {
                i = "panel-0",
                x = 3,
                y = 0,
                w = 6,
                h = 6,
                minW = 2,
                minH = 2,
                componentType = "favorites",
                componentConfig = new { showFavorites = true },
                metadata = new { title = "Favorites", description = "", createdAt = DateTime.UtcNow.ToString("o"), updatedAt = DateTime.UtcNow.ToString("o") }
            }
        };

        var saveV1Response = await _client.SendAsync(BuildAuthorizedRequest(HttpMethod.Post, $"/api/pages/{pageId}/layout", editorToken, layoutV1));
        Assert.Equal(HttpStatusCode.OK, saveV1Response.StatusCode);

        var saveV2Response = await _client.SendAsync(BuildAuthorizedRequest(HttpMethod.Post, $"/api/pages/{pageId}/layout", editorToken, layoutV2));
        Assert.Equal(HttpStatusCode.OK, saveV2Response.StatusCode);

        var versionsRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/pages/{pageId}/versions?take=10");
        versionsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", editorToken);
        var versionsResponse = await _client.SendAsync(versionsRequest);
        Assert.Equal(HttpStatusCode.OK, versionsResponse.StatusCode);

        var versionsJson = await versionsResponse.Content.ReadAsStringAsync();
        using var versionsDoc = JsonDocument.Parse(versionsJson);
        var versions = versionsDoc.RootElement;
        Assert.True(versions.GetArrayLength() >= 2);

        var olderVersionId = versions[1].GetProperty("id").GetInt32();

        var rollbackRequest = BuildAuthorizedRequest(HttpMethod.Post, $"/api/pages/{pageId}/versions/{olderVersionId}/rollback", editorToken, new
        {
            changeDescription = "Rollback during integration test"
        });
        var rollbackResponse = await _client.SendAsync(rollbackRequest);
        Assert.Equal(HttpStatusCode.OK, rollbackResponse.StatusCode);

        var pageRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/pages/{pageId}");
        pageRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", editorToken);
        var pageResponse = await _client.SendAsync(pageRequest);
        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);

        var pageJson = await pageResponse.Content.ReadAsStringAsync();
        using var pageDoc = JsonDocument.Parse(pageJson);
        var layoutRaw = pageDoc.RootElement.GetProperty("layout").GetString() ?? "[]";
        using var restoredLayoutDoc = JsonDocument.Parse(layoutRaw);

        var restoredX = restoredLayoutDoc.RootElement[0].GetProperty("x").GetInt32();
        Assert.Equal(0, restoredX);
    }

    private async Task<int> CreatePageAsync(string token, string title, bool isPublic)
    {
        var request = BuildAuthorizedRequest(HttpMethod.Post, "/api/pages", token, new
        {
            title,
            icon = "Document",
            parentId = (int?)null,
            order = 0,
            isPublic,
            layout = "[]",
            allowedUsers = Array.Empty<string>(),
            allowedGroups = Array.Empty<string>(),
            isDemo = false
        });

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("id").GetInt32();
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
