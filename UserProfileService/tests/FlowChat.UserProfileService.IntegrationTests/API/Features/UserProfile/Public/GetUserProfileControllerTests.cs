using System.Net;
using System.Net.Http.Json;
using FlowChat.UserProfileService.IntegrationTests.API;

namespace FlowChat.UserProfileService.IntegrationTests.API.Features.UserProfile.Public;

public sealed class GetUserProfileControllerTests(UserProfileApiFactory factory)
    : IClassFixture<UserProfileApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetById_WhenProfileExists_Returns200WithProfileData()
    {
        var userId = await CreateUserProfileAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/userprofiles/{userId:D}");
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, Guid.NewGuid().ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetUserProfileResponse>();
        body.Should().NotBeNull();
        body!.UserProfile.Id.Should().Be(userId);
        body.UserProfile.FriendlyUserId.Should().NotBeNullOrWhiteSpace();
        body.UserProfile.Emails.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_WhenProfileDoesNotExist_Returns404NotFound()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/userprofiles/{Guid.NewGuid():D}");
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, Guid.NewGuid().ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByEmail_WhenProfileExists_Returns200WithProfileData()
    {
        var (userId, email) = await CreateUserProfileWithEmailAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/userprofiles/by-email?email={Uri.EscapeDataString(email)}");
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, Guid.NewGuid().ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetUserProfileResponse>();
        body.Should().NotBeNull();
        body!.UserProfile.Id.Should().Be(userId);
        body.UserProfile.Emails.Should().Contain(e => e.Address == email);
    }

    [Fact]
    public async Task GetByEmail_WhenProfileDoesNotExist_Returns404NotFound()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/userprofiles/by-email?email=nonexistent@example.com");
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, Guid.NewGuid().ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByFriendlyUserId_WhenProfileExists_Returns200WithProfileData()
    {
        var (userId, _, friendlyUserId) = await CreateUserProfileWithFriendlyIdAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/userprofiles/by-friendly-id/{friendlyUserId}");
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, Guid.NewGuid().ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetUserProfileResponse>();
        body.Should().NotBeNull();
        body!.UserProfile.Id.Should().Be(userId);
        body.UserProfile.FriendlyUserId.Should().Be(friendlyUserId);
    }

    [Fact]
    public async Task GetByFriendlyUserId_WhenProfileDoesNotExist_Returns404NotFound()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/userprofiles/by-friendly-id/nonexistent-user");
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, Guid.NewGuid().ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> CreateUserProfileAsync()
    {
        var (userId, _, _) = await CreateUserProfileWithFriendlyIdAsync();
        return userId;
    }

    private async Task<(Guid UserId, string Email)> CreateUserProfileWithEmailAsync()
    {
        var userId = Guid.NewGuid();
        var email = $"gettest_{userId:N}@example.com";
        await factory.CreateInitialUserProfileAsync(
            userId,
            $"getuser-{userId:N}",
            email);
        return (userId, email);
    }

    private async Task<(Guid UserId, string Email, string FriendlyUserId)> CreateUserProfileWithFriendlyIdAsync()
    {
        var userId = Guid.NewGuid();
        var email = $"gettest_{userId:N}@example.com";
        var friendlyUserId = $"getuser-{userId:N}";
        await factory.CreateInitialUserProfileAsync(
            userId,
            friendlyUserId,
            email);
        return (userId, email, friendlyUserId);
    }

    private sealed record GetUserProfileResponse(UserProfileDto UserProfile);
    private sealed record UserProfileDto(Guid Id, string FriendlyUserId, List<EmailDto> Emails, List<PhoneDto> Phones);
    private sealed record EmailDto(Guid Id, string Address, bool IsMain, bool IsAuth, bool IsConfirmed);
    private sealed record PhoneDto(Guid Id, string Number, bool IsMain);
}
