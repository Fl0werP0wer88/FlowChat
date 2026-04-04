using System.Net;
using System.Net.Http.Json;
using FlowChat.UserProfileService.IntegrationTests.API;

namespace FlowChat.UserProfileService.IntegrationTests.API.Features.UserProfile.Public;

public sealed class AddPhoneControllerTests(UserProfileApiFactory factory)
    : IClassFixture<UserProfileApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task AddPhone_WithValidRequest_Returns200WithPhoneId()
    {
        var userId = await CreateUserProfileAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/userprofiles/{userId}/phones",
            new { Number = "+48123456789" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AddPhoneResponse>();
        body.Should().NotBeNull();
        body!.PhoneId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AddPhone_WhenProfileNotFound_Returns404NotFound()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/userprofiles/{Guid.NewGuid()}/phones",
            new { Number = "+48123456789" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddPhone_WithNullNumber_Returns400BadRequest()
    {
        var userId = await CreateUserProfileAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/userprofiles/{userId}/phones",
            new { Number = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddPhone_WithDuplicateNumber_Returns409Conflict()
    {
        var userId = await CreateUserProfileAsync();
        var phoneNumber = "+48500100200";

        await _client.PostAsJsonAsync($"/api/userprofiles/{userId}/phones", new { Number = phoneNumber });

        var response = await _client.PostAsJsonAsync(
            $"/api/userprofiles/{userId}/phones",
            new { Number = phoneNumber });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<Guid> CreateUserProfileAsync()
    {
        var userId = Guid.NewGuid();
        var request = new
        {
            UserId = userId,
            FriendlyUserId = $"phoneuser_{userId:N}",
            DisplayName = "Phone Test User",
            Email = $"phone_{userId:N}@example.com"
        };
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/internal/userprofiles/initial")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Internal-Api-Key", UserProfileApiFactory.InternalApiKey);
        await _client.SendAsync(httpRequest);
        return userId;
    }

    private sealed record AddPhoneResponse(Guid PhoneId);
}
