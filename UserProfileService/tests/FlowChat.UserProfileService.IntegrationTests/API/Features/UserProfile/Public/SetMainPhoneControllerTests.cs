using System.Net;
using System.Net.Http.Json;
using FlowChat.UserProfileService.IntegrationTests.API;

namespace FlowChat.UserProfileService.IntegrationTests.API.Features.UserProfile.Public;

public sealed class SetMainPhoneControllerTests(UserProfileApiFactory factory)
    : IClassFixture<UserProfileApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SetMainPhone_WhenProfileAndPhoneExist_Returns204NoContent()
    {
        var (userId, firstPhoneId, _) = await CreateProfileWithTwoPhonesAsync();

        var response = await _client.PutAsync(
            $"/api/userprofiles/{userId}/phones/{firstPhoneId}/main",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetMainPhone_WhenPhoneIsNotConfirmed_Returns400BadRequest()
    {
        var (userId, _, secondPhoneId) = await CreateProfileWithTwoPhonesAsync();

        var response = await _client.PutAsync(
            $"/api/userprofiles/{userId}/phones/{secondPhoneId}/main",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetMainPhone_WhenProfileNotFound_Returns404NotFound()
    {
        var response = await _client.PutAsync(
            $"/api/userprofiles/{Guid.NewGuid()}/phones/{Guid.NewGuid()}/main",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetMainPhone_WhenPhoneNotFound_Returns404NotFound()
    {
        var userId = await CreateProfileAsync();

        var response = await _client.PutAsync(
            $"/api/userprofiles/{userId}/phones/{Guid.NewGuid()}/main",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> CreateProfileAsync()
    {
        var userId = Guid.NewGuid();
        var request = new
        {
            UserId = userId,
            FriendlyUserId = $"phonesetuser-{userId:N}",
            Email = $"setphone_{userId:N}@example.com"
        };
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/internal/userprofiles/initial")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Internal-Api-Key", UserProfileApiFactory.InternalApiKey);
        await _client.SendAsync(httpRequest);
        return userId;
    }

    private async Task<(Guid UserId, Guid FirstPhoneId, Guid SecondPhoneId)> CreateProfileWithTwoPhonesAsync()
    {
        var userId = await CreateProfileAsync();

        var firstPhoneResponse = await _client.PostAsJsonAsync(
            $"/api/userprofiles/{userId}/phones",
            new { PhoneId = Guid.NewGuid(), Number = "+48100200300" });
        var firstPhone = await firstPhoneResponse.Content.ReadFromJsonAsync<AddPhoneResponse>();

        var addPhoneResponse = await _client.PostAsJsonAsync(
            $"/api/userprofiles/{userId}/phones",
            new { PhoneId = Guid.NewGuid(), Number = "+48400500600" });
        var addedPhone = await addPhoneResponse.Content.ReadFromJsonAsync<AddPhoneResponse>();

        return (userId, firstPhone!.PhoneId, addedPhone!.PhoneId);
    }
    private sealed record AddPhoneResponse(Guid PhoneId);
}
