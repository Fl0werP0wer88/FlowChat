using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.Presence.Eventing.ApplicationEvents.PresenceStatusChanged;
using FlowChat.Shared.Application;
using FluentAssertions;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class PresenceStatusChangedApplicationEventHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IContactObserverProjectionReadRepository> _readRepositoryMock = new();
    private readonly Mock<IOutboxIntegrationEventPublisher> _integrationEventPublisherMock = new();
    private readonly PresenceStatusChangedApplicationEventHandler _handler;

    public PresenceStatusChangedApplicationEventHandlerTests()
    {
        _handler = new PresenceStatusChangedApplicationEventHandler(
            _readRepositoryMock.Object,
            _integrationEventPublisherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenRecipientsExist_PublishesDistinctRecipientIntegrationEvent()
    {
        var userId = _fixture.Create<Guid>();
        var recipient1 = _fixture.Create<Guid>();
        var recipient2 = _fixture.Create<Guid>();
        var changedAtUtc = DateTimeOffset.UtcNow;
        IntegrationEventEnvelope<PresenceStatusChangedIntegrationEvent>? capturedEnvelope = null;

        _readRepositoryMock
            .Setup(x => x.GetObserverUserIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([recipient1, Guid.Empty, recipient1, recipient2]);
        _integrationEventPublisherMock
            .Setup(x => x.Publish(It.IsAny<IntegrationEventEnvelope<PresenceStatusChangedIntegrationEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<PresenceStatusChangedIntegrationEvent>, CancellationToken>((envelope, _) => capturedEnvelope = envelope)
            .Returns(Task.CompletedTask);

        await _handler.Handle(
            new PresenceStatusChangedApplicationEvent(userId, PresenceStatus.Busy, changedAtUtc),
            CancellationToken.None);

        capturedEnvelope.Should().NotBeNull();
        capturedEnvelope!.KafkaKey.Should().Be(userId.ToString("D"));
        var capturedEvent = capturedEnvelope.Payload;
        capturedEvent.UserId.Should().Be(userId);
        capturedEvent.Status.Should().Be(PresenceStatus.Busy);
        capturedEvent.ChangedAtUtc.Should().Be(changedAtUtc);
        capturedEvent.RecipientUserIds.Should().BeEquivalentTo([recipient1, recipient2]);
    }

    [Fact]
    public async Task Handle_WhenRecipientsAreEmpty_DoesNotPublishIntegrationEvent()
    {
        var userId = _fixture.Create<Guid>();

        _readRepositoryMock
            .Setup(x => x.GetObserverUserIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _handler.Handle(
            new PresenceStatusChangedApplicationEvent(userId, PresenceStatus.Active, DateTimeOffset.UtcNow),
            CancellationToken.None);

        _integrationEventPublisherMock.Verify(
            x => x.Publish(It.IsAny<IntegrationEventEnvelope<PresenceStatusChangedIntegrationEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRecipientsAreOnlyEmptyGuids_DoesNotPublishIntegrationEvent()
    {
        var userId = _fixture.Create<Guid>();

        _readRepositoryMock
            .Setup(x => x.GetObserverUserIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Guid.Empty, Guid.Empty]);

        await _handler.Handle(
            new PresenceStatusChangedApplicationEvent(userId, PresenceStatus.Invisible, DateTimeOffset.UtcNow),
            CancellationToken.None);

        _integrationEventPublisherMock.Verify(
            x => x.Publish(It.IsAny<IntegrationEventEnvelope<PresenceStatusChangedIntegrationEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
