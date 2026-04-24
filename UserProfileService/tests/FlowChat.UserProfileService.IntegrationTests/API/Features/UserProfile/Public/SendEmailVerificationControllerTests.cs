using System.Net;
using System.Net.Http.Json;
using FlowChat.UserProfileService.IntegrationTests.API;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.UserProfileService.IntegrationTests.API.Features.UserProfile.Public;

public sealed class SendEmailVerificationControllerTests(UserProfileApiFactory factory)
    : IClassFixture<UserProfileApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SendEmailVerification_WhenEmailIsUnconfirmed_Returns202Accepted()
    {
        var (userId, emailId) = await CreateProfileAndGetEmailIdAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/userprofiles/emails/{emailId}/verification");
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task SendEmailVerification_WhenProfileNotFound_Returns404NotFound()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/userprofiles/emails/{Guid.NewGuid()}/verification");
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, Guid.NewGuid().ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SendEmailVerification_WhenEmailNotFound_Returns404NotFound()
    {
        var userId = await CreateProfileAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/userprofiles/emails/{Guid.NewGuid()}/verification");
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SendEmailVerification_WhenEmailAlreadyConfirmed_Returns400BadRequest()
    {
        var (userId, emailId) = await CreateProfileAndGetEmailIdAsync();

        // Confirm the email first via the token flow
        await factory.WithDbContextAsync(async db =>
        {
            var typedUserId = FlowChat.Shared.Domain.Id<FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile>.FromGuid(userId);
            var profile = await db.UserProfiles
                .Include(p => p.Emails)
                .FirstAsync(p => p.Id == typedUserId);

            var email = profile.Emails.First(e => e.Id.Value == emailId);
            profile.ConfirmEmail(email.Id);
            await db.SaveChangesAsync();
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/userprofiles/emails/{emailId}/verification");
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<Guid> CreateProfileAsync()
    {
        var userId = Guid.NewGuid();
        var request = new
        {
            UserId = userId,
            FriendlyUserId = $"sendverif-{userId:N}",
            Email = $"sendverif_{userId:N}@example.com"
        };
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/internal/userprofiles/initial")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Internal-Api-Key", UserProfileApiFactory.InternalApiKey);
        await _client.SendAsync(httpRequest);
        return userId;
    }

    private async Task<(Guid UserId, Guid EmailId)> CreateProfileAndGetEmailIdAsync()
    {
        var userId = await CreateProfileAsync();

        var getProfile = new HttpRequestMessage(HttpMethod.Get, "/api/userprofiles");
        getProfile.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));
        var profileResponse = await _client.SendAsync(getProfile);
        var profile = await profileResponse.Content.ReadFromJsonAsync<GetUserProfileResponse>();
        var emailId = profile!.UserProfile.Emails[0].Id;

        return (userId, emailId);
    }

    private sealed record GetUserProfileResponse(UserProfileDto UserProfile);
    private sealed record UserProfileDto(Guid Id, List<EmailDto> Emails);
    private sealed record EmailDto(Guid Id, string Address, bool IsConfirmed);
}
