using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.RealtimeService.Consumers.Realtime.Contracts;
using FlowChat.RealtimeService.Consumers.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class UserPresenceChangedSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeInternalApiClient> _internalApiClientMock = new();
    private readonly UserPresenceChangedSubscriber _subscriber;

    public UserPresenceChangedSubscriberTests()
    {
        _subscriber = new UserPresenceChangedSubscriber(
            _internalApiClientMock.Object,
            NullLogger<UserPresenceChangedSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ForwardsNormalizedPresenceRequestToInternalApi()
    {
        PublishPresenceChangeRequest? capturedRequest = null;

        _internalApiClientMock
            .Setup(x => x.PublishPresenceChangeAsync(It.IsAny<PublishPresenceChangeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PublishPresenceChangeRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);

        await _subscriber.HandleAsync(
            new UserStatusChangedIntegrationEvent
            {
                UserId = _fixture.Create<Guid>(),
                Status = UserPresenceStatus.AFK,
                ChangedAtUtc = new DateTimeOffset(2026, 3, 17, 10, 15, 0, TimeSpan.Zero),
                RecipientUserIds = [_fixture.Create<Guid>()]
            },
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Status.Should().Be(UserPresenceStatus.AFK);
    }

    [Fact]
    public async Task HandleAsync_WhenStatusUnsupported_ThrowsNonTransientException()
    {
        var act = () => _subscriber.HandleAsync(
            new UserStatusChangedIntegrationEvent
            {
                UserId = _fixture.Create<Guid>(),
                Status = (UserPresenceStatus)999,
                RecipientUserIds = [_fixture.Create<Guid>()]
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _internalApiClientMock.Verify(
            x => x.PublishPresenceChangeAsync(It.IsAny<PublishPresenceChangeRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
