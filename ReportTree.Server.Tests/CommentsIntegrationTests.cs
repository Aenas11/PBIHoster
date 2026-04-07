using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ReportTree.Server.Tests;

public class CommentsIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CommentsIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CommentCrud_Works_ForOwner_AndAdminDelete()
    {
        var adminToken = await RegisterAndLoginAsync($"admin_{Guid.NewGuid():N}", "Password123!", new[] { "Admin" });
        await UpsertSettingAsync(adminToken, "App.CommentsEnabled", "true", "Application", "Enable comments feature");
        var ownerToken = await RegisterAndLoginAsync($"owner_{Guid.NewGuid():N}", "Password123!", new[] { "Viewer" });
        var otherToken = await RegisterAndLoginAsync($"other_{Guid.NewGuid():N}", "Password123!", new[] { "Viewer" });

        var pageId = await CreatePageAsync(adminToken, "Comments Page", isPublic: true);

        var createRequest = BuildAuthorizedRequest(HttpMethod.Post, $"/api/comments/page/{pageId}", ownerToken, new
        {
            content = "First comment @owner",
            parentId = (int?)null,
            mentions = new[] { "owner" }
        });

        var createResponse = await _client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var createdId = await ExtractIdAsync(createResponse);

        var anonymousGet = await _client.GetAsync($"/api/comments/page/{pageId}");
        Assert.Equal(HttpStatusCode.OK, anonymousGet.StatusCode);

        var ownerUpdate = BuildAuthorizedRequest(HttpMethod.Put, $"/api/comments/{createdId}", ownerToken, new
        {
            content = "Edited by owner @owner",
            mentions = new[] { "owner" }
        });

        var ownerUpdateResponse = await _client.SendAsync(ownerUpdate);
        Assert.Equal(HttpStatusCode.OK, ownerUpdateResponse.StatusCode);

        var otherUpdate = BuildAuthorizedRequest(HttpMethod.Put, $"/api/comments/{createdId}", otherToken, new
        {
            content = "Edited by other user",
            mentions = Array.Empty<string>()
        });

        var otherUpdateResponse = await _client.SendAsync(otherUpdate);
        Assert.Equal(HttpStatusCode.Forbidden, otherUpdateResponse.StatusCode);

        var adminDelete = new HttpRequestMessage(HttpMethod.Delete, $"/api/comments/{createdId}");
        adminDelete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var adminDeleteResponse = await _client.SendAsync(adminDelete);
        Assert.Equal(HttpStatusCode.OK, adminDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotRead_PrivatePageComments()
    {
        var adminToken = await RegisterAndLoginAsync($"admin_{Guid.NewGuid():N}", "Password123!", new[] { "Admin" });
        await UpsertSettingAsync(adminToken, "App.CommentsEnabled", "true", "Application", "Enable comments feature");
        var pageId = await CreatePageAsync(adminToken, "Private Comments", isPublic: false);

        var response = await _client.GetAsync($"/api/comments/page/{pageId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CommentsEndpoints_ReturnNotFound_WhenCommentsFeatureDisabled()
    {
        var adminToken = await RegisterAndLoginAsync($"admin_{Guid.NewGuid():N}", "Password123!", new[] { "Admin" });
        var viewerToken = await RegisterAndLoginAsync($"viewer_{Guid.NewGuid():N}", "Password123!", new[] { "Viewer" });
        var pageId = await CreatePageAsync(adminToken, "Comments Disabled Page", isPublic: true);

        await UpsertSettingAsync(adminToken, "App.CommentsEnabled", "false", "Application", "Disable comments feature");

        var getResponse = await _client.GetAsync($"/api/comments/page/{pageId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        var createRequest = BuildAuthorizedRequest(HttpMethod.Post, $"/api/comments/page/{pageId}", viewerToken, new
        {
            content = "Should not be created"
        });

        var createResponse = await _client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.NotFound, createResponse.StatusCode);
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

    private async Task UpsertSettingAsync(string token, string key, string value, string category, string description)
    {
        var request = BuildAuthorizedRequest(HttpMethod.Put, "/api/settings", token, new
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

    private static async Task<int> ExtractIdAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("id").GetInt32();
    }
}
