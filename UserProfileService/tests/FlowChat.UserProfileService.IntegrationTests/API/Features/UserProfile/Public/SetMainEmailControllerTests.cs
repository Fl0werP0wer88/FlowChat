using System.Net;
using System.Net.Http.Json;
using FlowChat.UserProfileService.IntegrationTests.API;

namespace FlowChat.UserProfileService.IntegrationTests.API.Features.UserProfile.Public;

public sealed class SetMainEmailControllerTests(UserProfileApiFactory factory)
    : IClassFixture<UserProfileApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SetMainEmail_WhenProfileAndEmailExist_Returns204NoContent()
    {
        var (userId, firstEmailId, _) = await CreateProfileWithTwoEmailsAsync();

        var response = await _client.PutAsync(
            $"/api/userprofiles/{userId}/emails/{firstEmailId}/main",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetMainEmail_WhenEmailIsNotConfirmed_Returns400BadRequest()
    {
        var (userId, _, secondEmailId) = await CreateProfileWithTwoEmailsAsync();

        var response = await _client.PutAsync(
            $"/api/userprofiles/{userId}/emails/{secondEmailId}/main",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetMainEmail_WhenProfileNotFound_Returns404NotFound()
    {
        var response = await _client.PutAsync(
            $"/api/userprofiles/{Guid.NewGuid()}/emails/{Guid.NewGuid()}/main",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetMainEmail_WhenEmailNotFound_Returns404NotFound()
    {
        var (userId, _, _) = await CreateProfileWithTwoEmailsAsync();

        var response = await _client.PutAsync(
            $"/api/userprofiles/{userId}/emails/{Guid.NewGuid()}/main",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<(Guid UserId, Guid FirstEmailId, Guid SecondEmailId)> CreateProfileWithTwoEmailsAsync()
    {
        var userId = Guid.NewGuid();
        var request = new
        {
            UserId = userId,
            FriendlyUserId = $"mainemailuser-{userId:N}",
            Email = $"first_{userId:N}@example.com"
        };
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/internal/userprofiles/initial")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Internal-Api-Key", UserProfileApiFactory.InternalApiKey);
        await _client.SendAsync(httpRequest);

        // Get the profile to get first email ID
        var profileResponse = await _client.GetAsync($"/api/userprofiles/{userId}");
        var profile = await profileResponse.Content.ReadFromJsonAsync<GetUserProfileResponse>();
        var firstEmailId = profile!.UserProfile.Emails[0].Id;

        // Add second email
        var addEmailResponse = await _client.PostAsJsonAsync(
            $"/api/userprofiles/{userId}/emails",
            new { Address = $"second_{userId:N}@example.com" });
        var addedEmail = await addEmailResponse.Content.ReadFromJsonAsync<AddEmailResponse>();

        return (userId, firstEmailId, addedEmail!.EmailId);
    }

    private sealed record GetUserProfileResponse(UserProfileDto UserProfile);
    private sealed record UserProfileDto(Guid Id, List<EmailDto> Emails);
    private sealed record EmailDto(Guid Id, string Address, bool IsMain);
    private sealed record AddEmailResponse(Guid EmailId);
}
