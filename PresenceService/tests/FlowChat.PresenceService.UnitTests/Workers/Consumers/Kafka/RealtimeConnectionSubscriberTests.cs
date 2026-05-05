using AutoFixture;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.RealtimeService.Events;
using FlowChat.PresenceService.Consumers.Kafka;
using FlowChat.PresenceService.Consumers.Configuration.Settings;
using FlowChat.PresenceService.Consumers.Presence.Contracts;
using FlowChat.PresenceService.Consumers.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class RealtimeConnectionRegisteredSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IPresenceInternalApiClient> _apiClientMock = new();
    private readonly RealtimeConnectionRegisteredSubscriber _subscriber;

    public RealtimeConnectionRegisteredSubscriberTests()
    {
        _subscriber = new RealtimeConnectionRegisteredSubscriber(
            _apiClientMock.Object,
            NullLogger<RealtimeConnectionRegisteredSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WhenFirstActiveConnection_InitializesPresence()
    {
        PresenceStatusRequest? capturedRequest = null;
        var userId = _fixture.Create<Guid>();

        _apiClientMock
            .Setup(x => x.InitializePresenceStatusAsync(It.IsAny<PresenceStatusRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PresenceStatusRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);

        await _subscriber.HandleAsync(
            new RealtimeConnectionRegisteredIntegrationEvent
            {
                UserId = userId,
                ConnectionId = "connection-1",
                ActiveConnectionCount = 1,
                IsFirstConnectionForUser = true,
                OccurredAtUtc = DateTimeOffset.UtcNow
            }.ToInboundEnvelope(),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task HandleAsync_WhenNotFirstConnectionForUser_DoesNotInitializePresence()
    {
        await _subscriber.HandleAsync(
            new RealtimeConnectionRegisteredIntegrationEvent
            {
                UserId = _fixture.Create<Guid>(),
                ConnectionId = "connection-2",
                ActiveConnectionCount = 1,
                IsFirstConnectionForUser = false,
                OccurredAtUtc = DateTimeOffset.UtcNow
            }.ToInboundEnvelope(),
            CancellationToken.None);

        _apiClientMock.Verify(
            x => x.InitializePresenceStatusAsync(It.IsAny<PresenceStatusRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenPayloadIsInvalid_ThrowsNonTransientException()
    {
        var act = () => _subscriber.HandleAsync(
            new RealtimeConnectionRegisteredIntegrationEvent
            {
                UserId = Guid.Empty,
                ConnectionId = "connection-3",
                ActiveConnectionCount = 1,
                IsFirstConnectionForUser = true,
                OccurredAtUtc = DateTimeOffset.UtcNow
            }.ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _apiClientMock.Verify(
            x => x.InitializePresenceStatusAsync(It.IsAny<PresenceStatusRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

public sealed class RealtimeConnectionUnregisteredSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IPresenceInternalApiClient> _apiClientMock = new();
    private readonly RealtimeConnectionUnregisteredSubscriber _subscriber;

    public RealtimeConnectionUnregisteredSubscriberTests()
    {
        _subscriber = new RealtimeConnectionUnregisteredSubscriber(
            _apiClientMock.Object,
            NullLogger<RealtimeConnectionUnregisteredSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WhenLastActiveConnectionIsRemoved_DeletesPresence()
    {
        PresenceStatusRequest? capturedRequest = null;
        var userId = _fixture.Create<Guid>();

        _apiClientMock
            .Setup(x => x.DeletePresenceStatusAsync(It.IsAny<PresenceStatusRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PresenceStatusRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);

        await _subscriber.HandleAsync(
            new RealtimeConnectionUnregisteredIntegrationEvent
            {
                UserId = userId,
                ConnectionId = "connection-4",
                ActiveConnectionCount = 0,
                IsLastConnectionForUser = true,
                OccurredAtUtc = DateTimeOffset.UtcNow
            }.ToInboundEnvelope(),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task HandleAsync_WhenNotLastConnectionForUser_DoesNotDeletePresence()
    {
        await _subscriber.HandleAsync(
            new RealtimeConnectionUnregisteredIntegrationEvent
            {
                UserId = _fixture.Create<Guid>(),
                ConnectionId = "connection-5",
                ActiveConnectionCount = 0,
                IsLastConnectionForUser = false,
                OccurredAtUtc = DateTimeOffset.UtcNow
            }.ToInboundEnvelope(),
            CancellationToken.None);

        _apiClientMock.Verify(
            x => x.DeletePresenceStatusAsync(It.IsAny<PresenceStatusRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenPayloadIsInvalid_ThrowsNonTransientException()
    {
        var act = () => _subscriber.HandleAsync(
            new RealtimeConnectionUnregisteredIntegrationEvent
            {
                UserId = _fixture.Create<Guid>(),
                ConnectionId = string.Empty,
                ActiveConnectionCount = 0,
                IsLastConnectionForUser = true,
                OccurredAtUtc = DateTimeOffset.UtcNow
            }.ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _apiClientMock.Verify(
            x => x.DeletePresenceStatusAsync(It.IsAny<PresenceStatusRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
