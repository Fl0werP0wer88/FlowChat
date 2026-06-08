using System.Net;
using System.Net.Http.Json;
using FlowChat.Core.Domain;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.Persistence.Entities;

namespace FlowChat.PresenceService.IntegrationTests.API.Features.Presence.Internal;

public sealed class DeletePresenceStatusControllerTests(PresenceApiFactory factory)
    : IClassFixture<PresenceApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Delete_WithValidInternalApiKey_RemovesPresenceAndPublishesInvisibleEvent()
    {
        factory.EventPublisher.Clear();

        var userId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();
        await factory.WithDbContextAsync(async db =>
        {
            await db.ContactObserverProjections.AddAsync(new ContactObserverReadModelEntity
            {
                ObservedUserId = userId,
                ObserverUserId = observerUserId
            });
            await db.SaveChangesAsync();
        });

        await factory.PresenceStatusStore.SetAsync(
            userId,
            PresenceStatus.Active,
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        var request = new HttpRequestMessage(HttpMethod.Delete, "/internal/presence/status/delete")
        {
            Content = JsonContent.Create(new { UserId = userId })
        };
        request.Headers.Add("X-Internal-Api-Key", PresenceApiFactory.InternalApiKey);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var storedStatus = await factory.PresenceStatusStore.GetAsync(userId, CancellationToken.None);
        storedStatus.Should().BeNull();
        var publishedEvent = factory.EventPublisher.PublishedOfType<PresenceStatusChangedIntegrationEvent>().Should().ContainSingle().Subject;
        publishedEvent.UserId.Should().Be(userId);
        publishedEvent.Status.Should().Be(PresenceStatus.Invisible);
        publishedEvent.RecipientUserIds.Should().BeEquivalentTo([observerUserId]);
    }

    [Fact]
    public async Task Delete_WhenUserHasNoObservers_RemovesPresenceWithoutPublishingEvent()
    {
        factory.EventPublisher.Clear();

        var userId = Guid.NewGuid();
        await factory.PresenceStatusStore.SetAsync(
            userId,
            PresenceStatus.Active,
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        var request = new HttpRequestMessage(HttpMethod.Delete, "/internal/presence/status/delete")
        {
            Content = JsonContent.Create(new { UserId = userId })
        };
        request.Headers.Add("X-Internal-Api-Key", PresenceApiFactory.InternalApiKey);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var storedStatus = await factory.PresenceStatusStore.GetAsync(userId, CancellationToken.None);
        storedStatus.Should().BeNull();
        factory.EventPublisher.PublishedOfType<PresenceStatusChangedIntegrationEvent>().Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_WithoutInternalApiKey_ReturnsUnauthorized()
    {
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/internal/presence/status/delete")
        {
            Content = JsonContent.Create(new { UserId = Guid.NewGuid() })
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_WhenPresenceDoesNotExist_ReturnsAcceptedWithoutPublishingEvent()
    {
        factory.EventPublisher.Clear();

        var request = new HttpRequestMessage(HttpMethod.Delete, "/internal/presence/status/delete")
        {
            Content = JsonContent.Create(new { UserId = Guid.NewGuid() })
        };
        request.Headers.Add("X-Internal-Api-Key", PresenceApiFactory.InternalApiKey);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        factory.EventPublisher.PublishedOfType<PresenceStatusChangedIntegrationEvent>().Should().BeEmpty();
    }
}
