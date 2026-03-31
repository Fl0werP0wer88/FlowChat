using System.Net;
using System.Net.Http.Json;
using FlowChat.UserProfileService.IntegrationTests.API;

namespace FlowChat.UserProfileService.IntegrationTests.API.Features.UserProfiles.Public;

public sealed class SetAuthEmailControllerTests(UserProfileApiFactory factory)
    : IClassFixture<UserProfileApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SetAuthEmail_WhenProfileAndEmailExist_Returns204NoContent()
    {
        var (userId, _, secondEmailId) = await CreateProfileWithTwoEmailsAsync();

        var response = await _client.PutAsync(
            $"/api/userprofiles/{userId}/emails/{secondEmailId}/auth",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetAuthEmail_WhenProfileNotFound_Returns404NotFound()
    {
        var response = await _client.PutAsync(
            $"/api/userprofiles/{Guid.NewGuid()}/emails/{Guid.NewGuid()}/auth",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetAuthEmail_WhenEmailNotFound_Returns404NotFound()
    {
        var userId = await CreateProfileAsync();

        var response = await _client.PutAsync(
            $"/api/userprofiles/{userId}/emails/{Guid.NewGuid()}/auth",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> CreateProfileAsync()
    {
        var userId = Guid.NewGuid();
        var request = new
        {
            UserId = userId,
            UserName = $"authuser_{userId:N}",
            DisplayName = "Auth Email Test User",
            Email = $"auth_{userId:N}@example.com"
        };
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/internal/userprofiles/initial")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Internal-Api-Key", UserProfileApiFactory.InternalApiKey);
        await _client.SendAsync(httpRequest);
        return userId;
    }

    private async Task<(Guid UserId, Guid FirstEmailId, Guid SecondEmailId)> CreateProfileWithTwoEmailsAsync()
    {
        var userId = Guid.NewGuid();
        var request = new
        {
            UserId = userId,
            UserName = $"authemailuser_{userId:N}",
            DisplayName = "Auth Email Test User",
            Email = $"authfirst_{userId:N}@example.com"
        };
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/internal/userprofiles/initial")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Internal-Api-Key", UserProfileApiFactory.InternalApiKey);
        await _client.SendAsync(httpRequest);

        var profileResponse = await _client.GetAsync($"/api/userprofiles/{userId}");
        var profile = await profileResponse.Content.ReadFromJsonAsync<GetUserProfileResponse>();
        var firstEmailId = profile!.UserProfile.Emails[0].Id;

        var addEmailResponse = await _client.PostAsJsonAsync(
            $"/api/userprofiles/{userId}/emails",
            new { Address = $"authsecond_{userId:N}@example.com" });
        var addedEmail = await addEmailResponse.Content.ReadFromJsonAsync<AddEmailResponse>();

        return (userId, firstEmailId, addedEmail!.EmailId);
    }

    private sealed record GetUserProfileResponse(UserProfileDto UserProfile);
    private sealed record UserProfileDto(Guid Id, List<EmailDto> Emails);
    private sealed record EmailDto(Guid Id, string Address, bool IsAuth);
    private sealed record AddEmailResponse(Guid EmailId);
}
