using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.RealtimeService.Consumers.Configuration.Settings;
using FlowChat.RealtimeService.Consumers.Realtime.Contracts;
using FlowChat.RealtimeService.Consumers.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class UserPresenceChangedSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeEventRouter> _eventRouterMock = new();
    private readonly UserPresenceChangedSubscriber _subscriber;

    public UserPresenceChangedSubscriberTests()
    {
        _subscriber = new UserPresenceChangedSubscriber(
            _eventRouterMock.Object,
            NullLogger<UserPresenceChangedSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ForwardsNormalizedPresenceRequestToRouter()
    {
        PublishPresenceChangeRequest? capturedRequest = null;

        _eventRouterMock
            .Setup(x => x.PublishPresenceChangeAsync(It.IsAny<PublishPresenceChangeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PublishPresenceChangeRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);

        await _subscriber.HandleAsync(
            new PresenceStatusChangedIntegrationEvent
            {
                UserId = _fixture.Create<Guid>(),
                Status = PresenceStatus.AFK,
                ChangedAtUtc = new DateTimeOffset(2026, 3, 17, 10, 15, 0, TimeSpan.Zero),
                RecipientUserIds = [_fixture.Create<Guid>()]
            },
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Status.Should().Be(PresenceStatus.AFK);
    }

    [Fact]
    public async Task HandleAsync_WhenStatusUnsupported_ThrowsNonTransientException()
    {
        var act = () => _subscriber.HandleAsync(
            new PresenceStatusChangedIntegrationEvent
            {
                UserId = _fixture.Create<Guid>(),
                Status = (PresenceStatus)999,
                RecipientUserIds = [_fixture.Create<Guid>()]
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _eventRouterMock.Verify(
            x => x.PublishPresenceChangeAsync(It.IsAny<PublishPresenceChangeRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
