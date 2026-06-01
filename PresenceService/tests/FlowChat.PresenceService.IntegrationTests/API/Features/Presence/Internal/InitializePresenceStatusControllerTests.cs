using System.Net;
using System.Net.Http.Json;
using FlowChat.Core.Domain;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.Persistence.Entities;

namespace FlowChat.PresenceService.IntegrationTests.API.Features.Presence.Internal;

public sealed class InitializePresenceStatusControllerTests(PresenceApiFactory factory)
    : IClassFixture<PresenceApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Initialize_WithValidInternalApiKey_StoresActiveStatusAndPublishesEvent()
    {
        factory.EventPublisher.Clear();

        var userId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();
        await factory.WithDbContextAsync(async db =>
        {
            await db.ContactObserverProjections.AddAsync(new ContactObserverReadModelEntity
            {
                ObservedUserId = userId,
                ObserverUserId = observerUserId,
                CreatedBy = "test",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                LastModifiedBy = "test",
                LastModifiedAtUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/internal/presence/status/initialize")
        {
            Content = JsonContent.Create(new { UserId = userId })
        };
        request.Headers.Add("X-Internal-Api-Key", PresenceApiFactory.InternalApiKey);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var storedStatus = await factory.PresenceStatusStore.GetAsync(userId, CancellationToken.None);
        storedStatus.Should().NotBeNull();
        storedStatus!.Status.Should().Be(PresenceStatus.Active);

        var publishedEvent = factory.EventPublisher.PublishedOfType<PresenceStatusChangedIntegrationEvent>().Should().ContainSingle().Subject;
        publishedEvent.UserId.Should().Be(userId);
        publishedEvent.Status.Should().Be(PresenceStatus.Active);
        publishedEvent.RecipientUserIds.Should().BeEquivalentTo([observerUserId]);
    }

    [Fact]
    public async Task Initialize_WhenUserHasNoObservers_StoresActiveStatusWithoutPublishingEvent()
    {
        factory.EventPublisher.Clear();

        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Post, "/internal/presence/status/initialize")
        {
            Content = JsonContent.Create(new { UserId = userId })
        };
        request.Headers.Add("X-Internal-Api-Key", PresenceApiFactory.InternalApiKey);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var storedStatus = await factory.PresenceStatusStore.GetAsync(userId, CancellationToken.None);
        storedStatus.Should().NotBeNull();
        storedStatus!.Status.Should().Be(PresenceStatus.Active);
        factory.EventPublisher.PublishedOfType<PresenceStatusChangedIntegrationEvent>().Should().BeEmpty();
    }

    [Fact]
    public async Task Initialize_WithoutInternalApiKey_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/internal/presence/status/initialize",
            new { UserId = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Initialize_WhenPresenceAlreadyExists_ReturnsAcceptedWithoutOverwriting()
    {
        factory.EventPublisher.Clear();

        var userId = Guid.NewGuid();
        var changedAtUtc = new DateTimeOffset(2026, 4, 13, 8, 30, 0, TimeSpan.Zero);
        await factory.PresenceStatusStore.SetAsync(
            userId,
            PresenceStatus.Busy,
            changedAtUtc,
            CancellationToken.None);

        var request = new HttpRequestMessage(HttpMethod.Post, "/internal/presence/status/initialize")
        {
            Content = JsonContent.Create(new { UserId = userId })
        };
        request.Headers.Add("X-Internal-Api-Key", PresenceApiFactory.InternalApiKey);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var storedStatus = await factory.PresenceStatusStore.GetAsync(userId, CancellationToken.None);
        storedStatus.Should().NotBeNull();
        storedStatus!.Status.Should().Be(PresenceStatus.Busy);
        storedStatus.ChangedAtUtc.Should().Be(changedAtUtc);
        factory.EventPublisher.PublishedOfType<PresenceStatusChangedIntegrationEvent>().Should().BeEmpty();
    }
}
