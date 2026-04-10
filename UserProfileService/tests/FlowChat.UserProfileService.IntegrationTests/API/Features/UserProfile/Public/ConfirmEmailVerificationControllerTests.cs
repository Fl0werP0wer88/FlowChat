using System.Net;
using System.Net.Http.Json;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification;
using FlowChat.UserProfileService.IntegrationTests.API;
using FlowChat.Shared.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.UserProfileService.IntegrationTests.API.Features.UserProfile.Public;

public sealed class ConfirmEmailVerificationControllerTests(UserProfileApiFactory factory)
    : IClassFixture<UserProfileApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ConfirmEmailVerification_WithValidToken_Returns204NoContent()
    {
        var (userId, emailId) = await CreateProfileAndGetEmailIdAsync();

        // Issue a verification request via the API
        await _client.PostAsync($"/api/userprofiles/{userId}/emails/{emailId}/verification", content: null);

        // Read the nonce from the DB and construct the token
        var token = await BuildTokenFromDbAsync(userId, emailId);

        var response = await _client.PostAsJsonAsync(
            "/api/userprofiles/email-verification/confirm",
            new { Token = token });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ConfirmEmailVerification_WithInvalidToken_Returns400BadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/userprofiles/email-verification/confirm",
            new { Token = "this-is-not-a-valid-token" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConfirmEmailVerification_WithEmptyToken_Returns400BadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/userprofiles/email-verification/confirm",
            new { Token = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConfirmEmailVerification_WithAlreadyConsumedToken_Returns400BadRequest()
    {
        var (userId, emailId) = await CreateProfileAndGetEmailIdAsync();

        await _client.PostAsync($"/api/userprofiles/{userId}/emails/{emailId}/verification", content: null);
        var token = await BuildTokenFromDbAsync(userId, emailId);

        // Confirm successfully
        await _client.PostAsJsonAsync("/api/userprofiles/email-verification/confirm", new { Token = token });

        // Try to confirm again with the same (now consumed) token
        var response = await _client.PostAsJsonAsync(
            "/api/userprofiles/email-verification/confirm",
            new { Token = token });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<(Guid UserId, Guid EmailId)> CreateProfileAndGetEmailIdAsync()
    {
        var userId = Guid.NewGuid();
        var request = new
        {
            UserId = userId,
            FriendlyUserId = $"confirmverif-{userId:N}",
            Email = $"confirmverif_{userId:N}@example.com"
        };
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/internal/userprofiles/initial")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Internal-Api-Key", UserProfileApiFactory.InternalApiKey);
        await _client.SendAsync(httpRequest);

        var profileResponse = await _client.GetAsync($"/api/userprofiles/{userId}");
        var profile = await profileResponse.Content.ReadFromJsonAsync<GetUserProfileResponse>();
        var emailId = profile!.UserProfile.Emails[0].Id;

        return (userId, emailId);
    }

    private async Task<string> BuildTokenFromDbAsync(Guid userId, Guid emailId)
    {
        // Read the active verification request created for this test run. Profile creation
        // already issues an earlier request, so we need the latest active one.
        string nonce = await factory.WithDbContextAsync(async db =>
        {
            var typedUserId = FlowChat.Shared.Domain.Id<FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile>.FromGuid(userId);
            var typedEmailId = FlowChat.Shared.Domain.Id<FlowChat.UserProfileService.Domain.Entities.UserProfile.Email>.FromGuid(emailId);
            var nowUtc = UtcDateTimeOffset.UtcNow;
            var query = db.EmailVerificationRequests
                .Where(r => r.UserProfileId == typedUserId
                            && r.EmailId == typedEmailId
                            && r.InvalidatedAtUtc == null
                            && r.ConsumedAtUtc == null);

            var verificationRequest = string.Equals(
                db.Database.ProviderName,
                "Microsoft.EntityFrameworkCore.Sqlite",
                StringComparison.Ordinal)
                ? (await query.ToListAsync())
                    .Where(r => r.ExpiresAtUtc > nowUtc)
                    .OrderByDescending(r => r.ExpiresAtUtc)
                    .First()
                : await query
                    .Where(r => r.ExpiresAtUtc > nowUtc)
                    .OrderByDescending(r => r.ExpiresAtUtc)
                    .FirstAsync();
            return verificationRequest.Nonce;
        });

        using var scope = factory.Services.CreateScope();
        var tokenProtector = scope.ServiceProvider.GetRequiredService<IEmailVerificationTokenProtector>();
        return tokenProtector.Protect(new EmailVerificationTokenPayload(userId, emailId, nonce));
    }

    private sealed record GetUserProfileResponse(UserProfileDto UserProfile);
    private sealed record UserProfileDto(Guid Id, List<EmailDto> Emails);
    private sealed record EmailDto(Guid Id, string Address, bool IsConfirmed);
}
