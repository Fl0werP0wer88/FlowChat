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

        var response = await _client.PostAsJsonAsync(
            $"/api/userprofiles/{userId}/emails",
            new { Address = $"new_{userId:N}@example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AddEmailResponse>();
        body.Should().NotBeNull();
        body!.EmailId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AddEmail_WhenProfileNotFound_Returns404NotFound()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/userprofiles/{Guid.NewGuid()}/emails",
            new { Address = "any@example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddEmail_WhenEmailAlreadyExists_Returns409Conflict()
    {
        var userId = await CreateUserProfileAsync();
        var duplicateEmail = $"dup_{userId:N}@example.com";

        // Add the email once
        await _client.PostAsJsonAsync(
            $"/api/userprofiles/{userId}/emails",
            new { Address = duplicateEmail });

        // Try to add the same email again (to any profile)
        var response = await _client.PostAsJsonAsync(
            $"/api/userprofiles/{userId}/emails",
            new { Address = duplicateEmail });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddEmail_WithInvalidEmailFormat_Returns400BadRequest()
    {
        var userId = await CreateUserProfileAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/userprofiles/{userId}/emails",
            new { Address = "not-a-valid-email" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddEmail_WithNullAddress_Returns400BadRequest()
    {
        var userId = await CreateUserProfileAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/userprofiles/{userId}/emails",
            new { Address = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<Guid> CreateUserProfileAsync()
    {
        var userId = Guid.NewGuid();
        var request = new
        {
            UserId = userId,
            FriendlyUserId = $"emailuser_{userId:N}",
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
