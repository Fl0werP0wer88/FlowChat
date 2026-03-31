using System.Net;
using System.Net.Http.Json;
using FlowChat.UserProfileService.IntegrationTests.API;

namespace FlowChat.UserProfileService.IntegrationTests.API.Features.UserProfiles.Public;

public sealed class GetUserProfileControllerTests(UserProfileApiFactory factory)
    : IClassFixture<UserProfileApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetById_WhenProfileExists_Returns200WithProfileData()
    {
        var userId = await CreateUserProfileAsync();

        var response = await _client.GetAsync($"/api/userprofiles/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetUserProfileResponse>();
        body.Should().NotBeNull();
        body!.UserProfile.Id.Should().Be(userId);
        body.UserProfile.UserName.Should().NotBeNullOrWhiteSpace();
        body.UserProfile.Emails.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_WhenProfileDoesNotExist_Returns404NotFound()
    {
        var response = await _client.GetAsync($"/api/userprofiles/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> CreateUserProfileAsync()
    {
        var userId = Guid.NewGuid();
        var request = new
        {
            UserId = userId,
            UserName = $"getuser_{userId:N}",
            DisplayName = "Get Test User",
            Email = $"gettest_{userId:N}@example.com"
        };
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/internal/userprofiles/initial")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Internal-Api-Key", UserProfileApiFactory.InternalApiKey);
        await _client.SendAsync(httpRequest);
        return userId;
    }

    private sealed record GetUserProfileResponse(UserProfileDto UserProfile);
    private sealed record UserProfileDto(Guid Id, string UserName, string DisplayName, List<EmailDto> Emails, List<PhoneDto> Phones);
    private sealed record EmailDto(Guid Id, string Address, bool IsMain, bool IsAuth, bool IsConfirmed);
    private sealed record PhoneDto(Guid Id, string Number, bool IsMain);
}
