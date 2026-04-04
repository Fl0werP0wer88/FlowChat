using System.Net;
using System.Net.Http.Json;
using FlowChat.UserProfileService.IntegrationTests.API;

namespace FlowChat.UserProfileService.IntegrationTests.API.Features.UserProfile.Internal;

public sealed class CreateInitialUserProfileControllerTests(UserProfileApiFactory factory)
    : IClassFixture<UserProfileApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateInitialUserProfile_WithValidApiKey_Returns202Accepted()
    {
        var userId = Guid.NewGuid();
        var request = new
        {
            UserId = userId,
            FriendlyUserId = $"testuser_{userId:N}",
            DisplayName = "Test User",
            Email = $"test_{userId:N}@example.com"
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/internal/userprofiles/initial")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Internal-Api-Key", UserProfileApiFactory.InternalApiKey);

        var response = await _client.SendAsync(httpRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task CreateInitialUserProfile_WithoutApiKey_Returns401Unauthorized()
    {
        var userId = Guid.NewGuid();
        var request = new
        {
            UserId = userId,
            FriendlyUserId = $"testuser_{userId:N}",
            DisplayName = "Test User",
            Email = $"test_{userId:N}@example.com"
        };

        var response = await _client.PostAsJsonAsync("/internal/userprofiles/initial", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateInitialUserProfile_WithWrongApiKey_Returns401Unauthorized()
    {
        var userId = Guid.NewGuid();
        var request = new
        {
            UserId = userId,
            FriendlyUserId = $"testuser_{userId:N}",
            DisplayName = "Test User",
            Email = $"test_{userId:N}@example.com"
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/internal/userprofiles/initial")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Internal-Api-Key", "wrong-key");

        var response = await _client.SendAsync(httpRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateInitialUserProfile_WithDuplicateUserId_Returns409Conflict()
    {
        var userId = Guid.NewGuid();
        var request = new
        {
            UserId = userId,
            FriendlyUserId = $"dupuser_{userId:N}",
            DisplayName = "Dup User",
            Email = $"dup_{userId:N}@example.com"
        };

        var httpRequest1 = new HttpRequestMessage(HttpMethod.Post, "/internal/userprofiles/initial")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest1.Headers.Add("X-Internal-Api-Key", UserProfileApiFactory.InternalApiKey);
        await _client.SendAsync(httpRequest1);

        // Second request with same userId
        var httpRequest2 = new HttpRequestMessage(HttpMethod.Post, "/internal/userprofiles/initial")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest2.Headers.Add("X-Internal-Api-Key", UserProfileApiFactory.InternalApiKey);
        var response = await _client.SendAsync(httpRequest2);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateInitialUserProfile_WithMissingEmail_Returns400BadRequest()
    {
        var userId = Guid.NewGuid();
        var request = new
        {
            UserId = userId,
            FriendlyUserId = $"noemail_{userId:N}",
            DisplayName = "No Email",
            Email = (string?)null
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/internal/userprofiles/initial")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Internal-Api-Key", UserProfileApiFactory.InternalApiKey);

        var response = await _client.SendAsync(httpRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
