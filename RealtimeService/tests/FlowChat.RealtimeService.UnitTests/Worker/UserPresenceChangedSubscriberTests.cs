using FlowChat.Messaging.Contracts.UserProfileService.Events;
using FlowChat.RealtimeService.Consumers.Kafka;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class UserPresenceChangedSubscriberTests
{
    [Fact]
    public async Task HandleAsync_ForwardsNormalizedPresenceRequestToInternalApi()
    {
        var internalApiClient = new CapturingRealtimeInternalApiClient();
        var subscriber = new UserPresenceChangedSubscriber(
            internalApiClient,
            NullLogger<UserPresenceChangedSubscriber>.Instance);

        await subscriber.HandleAsync(
            new UserPresenceChangedIntegrationEvent
            {
                UserId = Guid.NewGuid(),
                Status = " Away ",
                ChangedAtUtc = new DateTime(2026, 3, 17, 10, 15, 0, DateTimeKind.Utc),
                RecipientUserIds = [Guid.NewGuid()]
            },
            CancellationToken.None);

        Assert.NotNull(internalApiClient.LastPublishPresenceChangeRequest);
        Assert.Equal("away", internalApiClient.LastPublishPresenceChangeRequest!.Status);
    }

    [Fact]
    public async Task HandleAsync_WhenStatusUnsupported_ThrowsInvalidOperationException()
    {
        var internalApiClient = new CapturingRealtimeInternalApiClient();
        var subscriber = new UserPresenceChangedSubscriber(
            internalApiClient,
            NullLogger<UserPresenceChangedSubscriber>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => subscriber.HandleAsync(
            new UserPresenceChangedIntegrationEvent
            {
                UserId = Guid.NewGuid(),
                Status = "busy",
                RecipientUserIds = [Guid.NewGuid()]
            },
            CancellationToken.None));

        Assert.Null(internalApiClient.LastPublishPresenceChangeRequest);
    }
}
