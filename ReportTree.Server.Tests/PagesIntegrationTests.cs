using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ReportTree.Server.Tests;

public class PagesIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PagesIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AnonymousUser_CanAccessPublicPage_ButNotPrivatePage()
    {
        var adminToken = await RegisterAndLoginAsync($"admin_{Guid.NewGuid():N}", "Password123!", new[] { "Admin" });

        var publicPageId = await CreatePageAsync(adminToken, "Public Page", isPublic: true);
        var privatePageId = await CreatePageAsync(adminToken, "Private Page", isPublic: false);

        var publicResponse = await _client.GetAsync($"/api/pages/{publicPageId}");
        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);

        var privateResponse = await _client.GetAsync($"/api/pages/{privatePageId}");
        Assert.Equal(HttpStatusCode.Unauthorized, privateResponse.StatusCode);
    }

    [Fact]
    public async Task Viewer_CannotCreatePage_ButEditorCan()
    {
        var editorToken = await RegisterAndLoginAsync($"editor_{Guid.NewGuid():N}", "Password123!", new[] { "Editor" });
        var viewerToken = await RegisterAndLoginAsync($"viewer_{Guid.NewGuid():N}", "Password123!", new[] { "Viewer" });

        var viewerRequest = BuildAuthorizedRequest(HttpMethod.Post, "/api/pages", viewerToken, new
        {
            title = "Viewer Attempt",
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

        var viewerResponse = await _client.SendAsync(viewerRequest);
        Assert.Equal(HttpStatusCode.Forbidden, viewerResponse.StatusCode);

        var editorRequest = BuildAuthorizedRequest(HttpMethod.Post, "/api/pages", editorToken, new
        {
            title = "Editor Page",
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

        var editorResponse = await _client.SendAsync(editorRequest);
        Assert.Equal(HttpStatusCode.Created, editorResponse.StatusCode);
    }

    [Fact]
    public async Task EnforcedSensitivityLabels_RejectsMissingOrInvalidLabel()
    {
        var adminToken = await RegisterAndLoginAsync($"admin_{Guid.NewGuid():N}", "Password123!", new[] { "Admin" });
        await UpsertSettingAsync(adminToken, "App.EnforceSensitivityLabels", "true", "Application", "Require page sensitivity labels");

        var invalidLabelRequest = BuildAuthorizedRequest(HttpMethod.Post, "/api/pages", adminToken, new
        {
            title = "Invalid Label Page",
            icon = "Document",
            parentId = (int?)null,
            order = 0,
            isPublic = true,
            sensitivityLabel = "TopSecret",
            layout = "[]",
            allowedUsers = Array.Empty<string>(),
            allowedGroups = Array.Empty<string>(),
            isDemo = false
        });

        var invalidLabelResponse = await _client.SendAsync(invalidLabelRequest);
        Assert.Equal(HttpStatusCode.BadRequest, invalidLabelResponse.StatusCode);

        var emptyLabelRequest = BuildAuthorizedRequest(HttpMethod.Post, "/api/pages", adminToken, new
        {
            title = "Empty Label Page",
            icon = "Document",
            parentId = (int?)null,
            order = 0,
            isPublic = true,
            sensitivityLabel = "",
            layout = "[]",
            allowedUsers = Array.Empty<string>(),
            allowedGroups = Array.Empty<string>(),
            isDemo = false
        });

        var emptyLabelResponse = await _client.SendAsync(emptyLabelRequest);
        Assert.Equal(HttpStatusCode.BadRequest, emptyLabelResponse.StatusCode);

        await UpsertSettingAsync(adminToken, "App.EnforceSensitivityLabels", "false", "Application", "Disable label enforcement");
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
            sensitivityLabel = "Internal",
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
}
