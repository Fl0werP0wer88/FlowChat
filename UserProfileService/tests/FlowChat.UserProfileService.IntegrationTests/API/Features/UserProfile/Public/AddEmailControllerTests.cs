using System.Net;
using System.Net.Http.Json;
using FlowChat.UserProfileService.IntegrationTests.API;

namespace FlowChat.UserProfileService.IntegrationTests.API.Features.UserProfile.Public;

public sealed class AddEmailControllerTests(UserProfileApiFactory factory)
    : IClassFixture<UserProfileApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task AddEmail_WithValidRequest_Returns200WithEmailId()
    {
        var userId = await CreateUserProfileAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/userprofiles/emails")
        {
            Content = JsonContent.Create(new { EmailId = Guid.NewGuid(), Address = $"new_{userId:N}@example.com" })
        };
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AddEmailResponse>();
        body.Should().NotBeNull();
        body!.EmailId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AddEmail_WhenProfileNotFound_Returns404NotFound()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/userprofiles/emails")
        {
            Content = JsonContent.Create(new { Address = "any@example.com" })
        };
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, Guid.NewGuid().ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddEmail_WhenEmailAlreadyExists_Returns409Conflict()
    {
        var userId = await CreateUserProfileAsync();
        var duplicateEmail = $"dup_{userId:N}@example.com";

        var addFirst = new HttpRequestMessage(HttpMethod.Post, "/api/userprofiles/emails")
        {
            Content = JsonContent.Create(new { EmailId = Guid.NewGuid(), Address = duplicateEmail })
        };
        addFirst.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));
        await _client.SendAsync(addFirst);

        var addSecond = new HttpRequestMessage(HttpMethod.Post, "/api/userprofiles/emails")
        {
            Content = JsonContent.Create(new { EmailId = Guid.NewGuid(), Address = duplicateEmail })
        };
        addSecond.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));
        var response = await _client.SendAsync(addSecond);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddEmail_WithInvalidEmailFormat_Returns400BadRequest()
    {
        var userId = await CreateUserProfileAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/userprofiles/emails")
        {
            Content = JsonContent.Create(new { EmailId = Guid.NewGuid(), Address = "not-a-valid-email" })
        };
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddEmail_WithNullAddress_Returns400BadRequest()
    {
        var userId = await CreateUserProfileAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/userprofiles/emails")
        {
            Content = JsonContent.Create(new { EmailId = Guid.NewGuid(), Address = (string?)null })
        };
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<Guid> CreateUserProfileAsync()
    {
        var userId = Guid.NewGuid();
        var request = new
        {
            UserId = userId,
            FriendlyUserId = $"emailuser-{userId:N}",
            Email = $"initial_{userId:N}@example.com"
        };
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/internal/userprofiles/initial")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Internal-Api-Key", UserProfileApiFactory.InternalApiKey);
        await _client.SendAsync(httpRequest);
        return userId;
    }

    private sealed record AddEmailResponse(Guid EmailId);
}
