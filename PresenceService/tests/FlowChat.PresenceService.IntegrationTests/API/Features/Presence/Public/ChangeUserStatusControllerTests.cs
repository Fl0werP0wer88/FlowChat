using System.Net;
using System.Net.Http.Json;
using FlowChat.Core.Domain;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.Persistence.Entities;

namespace FlowChat.PresenceService.IntegrationTests.API.Features.Presence.Public;

public sealed class ChangeUserStatusControllerTests(PresenceApiFactory factory)
    : IClassFixture<PresenceApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ChangeUserStatus_WithAuthenticatedUser_StoresStatusAndPublishesEvent()
    {
        var userId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();

        await factory.WithDbContextAsync(async db =>
        {
            await db.ContactObserverProjections.AddAsync(new ContactObserverProjectionEntity
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

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/presence/status")
        {
            Content = JsonContent.Create(new { Status = UserPresenceStatus.Busy })
        };
        request.Headers.Add(TestAuthenticationHandler.UserIdHeaderName, userId.ToString("D"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var storedStatus = await factory.PresenceStatusStore.GetAsync(userId, CancellationToken.None);
        storedStatus.Should().NotBeNull();
        storedStatus!.Status.Should().Be(UserPresenceStatus.Busy);

        var publishedEvent = factory.EventPublisher.PublishedOfType<UserStatusChangedIntegrationEvent>().Should().ContainSingle().Subject;
        publishedEvent.UserId.Should().Be(userId);
        publishedEvent.Status.Should().Be(UserPresenceStatus.Busy);
        publishedEvent.RecipientUserIds.Should().BeEquivalentTo([observerUserId]);
    }
}
