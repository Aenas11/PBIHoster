using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ReportTree.Server.Tests;

public class ProfileFavoritesIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProfileFavoritesIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Favorites_AddGetRemove_Works()
    {
        var adminToken = await RegisterAndLoginAsync($"admin_{Guid.NewGuid():N}", "Password123!", new[] { "Admin" });
        var viewerToken = await RegisterAndLoginAsync($"viewer_{Guid.NewGuid():N}", "Password123!", new[] { "Viewer" });

        var pageId = await CreatePublicPageAsync(adminToken, "Favorite Candidate");

        var addRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/profile/favorites/{pageId}");
        addRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", viewerToken);
        var addResponse = await _client.SendAsync(addRequest);
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        var getRequest = new HttpRequestMessage(HttpMethod.Get, "/api/profile/favorites");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", viewerToken);
        var getResponse = await _client.SendAsync(getRequest);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getBody = await getResponse.Content.ReadAsStringAsync();
        var favorites = JsonSerializer.Deserialize<List<int>>(getBody) ?? new List<int>();
        Assert.Contains(pageId, favorites);

        var removeRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/profile/favorites/{pageId}");
        removeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", viewerToken);
        var removeResponse = await _client.SendAsync(removeRequest);
        Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);

        var getAfterRemoveRequest = new HttpRequestMessage(HttpMethod.Get, "/api/profile/favorites");
        getAfterRemoveRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", viewerToken);
        var getAfterRemoveResponse = await _client.SendAsync(getAfterRemoveRequest);
        Assert.Equal(HttpStatusCode.OK, getAfterRemoveResponse.StatusCode);
        var afterRemoveBody = await getAfterRemoveResponse.Content.ReadAsStringAsync();
        var afterRemoveFavorites = JsonSerializer.Deserialize<List<int>>(afterRemoveBody) ?? new List<int>();
        Assert.DoesNotContain(pageId, afterRemoveFavorites);
    }

    private async Task<int> CreatePublicPageAsync(string token, string title)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/pages")
        {
            Content = JsonContent.Create(new
            {
                title,
                icon = "Document",
                parentId = (int?)null,
                order = 0,
                isPublic = true,
                layout = "[]",
                allowedUsers = Array.Empty<string>(),
                allowedGroups = Array.Empty<string>(),
                isDemo = false
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

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

        var body = await loginResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("token").GetString() ?? string.Empty;
    }
}
