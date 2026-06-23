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

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/userprofiles/emails/{firstEmailId}/main");
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetMainEmail_WhenEmailIsNotConfirmed_Returns400BadRequest()
    {
        var (userId, _, secondEmailId) = await CreateProfileWithTwoEmailsAsync();

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/userprofiles/emails/{secondEmailId}/main");
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetMainEmail_WhenProfileNotFound_Returns404NotFound()
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/userprofiles/emails/{Guid.NewGuid()}/main");
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, Guid.NewGuid().ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetMainEmail_WhenEmailNotFound_Returns404NotFound()
    {
        var (userId, _, _) = await CreateProfileWithTwoEmailsAsync();

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/userprofiles/emails/{Guid.NewGuid()}/main");
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<(Guid UserId, Guid FirstEmailId, Guid SecondEmailId)> CreateProfileWithTwoEmailsAsync()
    {
        var userId = Guid.NewGuid();
        var createRequest = new
        {
            UserId = userId,
            FriendlyUserId = $"mainemailuser-{userId:N}",
            Email = $"first_{userId:N}@example.com"
        };
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/internal/userprofiles/initial")
        {
            Content = JsonContent.Create(createRequest)
        };
        httpRequest.Headers.Add("X-Internal-Api-Key", UserProfileApiFactory.InternalApiKey);
        await _client.SendAsync(httpRequest);

        var getProfile = new HttpRequestMessage(HttpMethod.Get, $"/api/userprofiles/{userId:D}");
        getProfile.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));
        var profileResponse = await _client.SendAsync(getProfile);
        var profile = await profileResponse.Content.ReadFromJsonAsync<GetUserProfileResponse>();
        var firstEmailId = profile!.UserProfile.Emails[0].Id;

        var addEmail = new HttpRequestMessage(HttpMethod.Put, "/api/userprofiles/emails")
        {
            Content = JsonContent.Create(new { Address = $"second_{userId:N}@example.com" })
        };
        addEmail.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));
        var addEmailResponse = await _client.SendAsync(addEmail);
        var addedEmail = await addEmailResponse.Content.ReadFromJsonAsync<AddEmailResponse>();

        return (userId, firstEmailId, addedEmail!.EmailId);
    }

    private sealed record GetUserProfileResponse(UserProfileDto UserProfile);
    private sealed record UserProfileDto(Guid Id, List<EmailDto> Emails);
    private sealed record EmailDto(Guid Id, string Address, bool IsMain);
    private sealed record AddEmailResponse(Guid EmailId);
}
