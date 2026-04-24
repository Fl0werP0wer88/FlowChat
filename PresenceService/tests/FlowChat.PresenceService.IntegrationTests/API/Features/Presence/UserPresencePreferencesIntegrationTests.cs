using System.Net;
using System.Net.Http.Json;
using FlowChat.Core.Domain;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.Domain.Entities.UserPresencePreferences;
using FlowChat.PresenceService.Persistence;
using FlowChat.Shared.Domain;

namespace FlowChat.PresenceService.IntegrationTests.API.Features.Presence;

public sealed class UserPresencePreferencesIntegrationTests(PresenceApiFactory factory)
    : IClassFixture<PresenceApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData(PresenceStatus.Busy)]
    [InlineData(PresenceStatus.Invisible)]
    public async Task ChangeStatus_ToManualStatus_PersistsPreference(PresenceStatus status)
    {
        var userId = Guid.NewGuid();

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/presence/status")
        {
            Content = JsonContent.Create(new { Status = status })
        };
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var preferredStatus = await factory.WithDbContextAsync(async db =>
        {
            var entity = await db.UserPresencePreferences.FindAsync(Id<UserPresencePreferences>.FromGuid(userId));
            return entity?.PreferredStatus;
        });

        preferredStatus.Should().Be(status);
    }

    [Fact]
    public async Task ChangeStatus_ToActive_DeletesPreference()
    {
        var userId = Guid.NewGuid();

        // First set Busy to create the preference row
        var busyRequest = new HttpRequestMessage(HttpMethod.Put, "/api/presence/status")
        {
            Content = JsonContent.Create(new { Status = PresenceStatus.Busy })
        };
        busyRequest.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));
        await _client.SendAsync(busyRequest);

        // Then switch back to Active
        var activeRequest = new HttpRequestMessage(HttpMethod.Put, "/api/presence/status")
        {
            Content = JsonContent.Create(new { Status = PresenceStatus.Active })
        };
        activeRequest.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));
        var response = await _client.SendAsync(activeRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var preferredStatus = await factory.WithDbContextAsync(async db =>
        {
            var entity = await db.UserPresencePreferences.FindAsync(Id<UserPresencePreferences>.FromGuid(userId));
            return entity?.PreferredStatus;
        });

        preferredStatus.Should().BeNull();
    }

    [Theory]
    [InlineData(PresenceStatus.Busy)]
    [InlineData(PresenceStatus.Invisible)]
    public async Task Initialize_WhenPreferenceExists_RestoresPreferredStatus(PresenceStatus preferredStatus)
    {
        factory.EventPublisher.Clear();

        var userId = Guid.NewGuid();

        // Set the manual preference via ChangePresenceStatus
        var setRequest = new HttpRequestMessage(HttpMethod.Put, "/api/presence/status")
        {
            Content = JsonContent.Create(new { Status = preferredStatus })
        };
        setRequest.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));
        await _client.SendAsync(setRequest);

        // Simulate Redis TTL expiry / crash: clear the in-memory presence store
        await factory.PresenceStatusStore.DeleteAsync(userId, CancellationToken.None);
        factory.EventPublisher.Clear();

        // Reconnect
        var initRequest = new HttpRequestMessage(HttpMethod.Post, "/internal/presence/status/initialize")
        {
            Content = JsonContent.Create(new { UserId = userId })
        };
        initRequest.Headers.Add("X-Internal-Api-Key", PresenceApiFactory.InternalApiKey);

        var response = await _client.SendAsync(initRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var storedStatus = await factory.PresenceStatusStore.GetAsync(userId, CancellationToken.None);
        storedStatus.Should().NotBeNull();
        storedStatus!.Status.Should().Be(preferredStatus);

        var publishedEvent = factory.EventPublisher
            .PublishedOfType<PresenceStatusChangedIntegrationEvent>()
            .Should().ContainSingle().Subject;
        publishedEvent.UserId.Should().Be(userId);
        publishedEvent.Status.Should().Be(preferredStatus);
    }
}
