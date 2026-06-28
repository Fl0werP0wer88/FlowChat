using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.Core.Results;
using FlowChat.RealtimeService.Application.Features.Presence.Commands.RoutePresenceChange;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Unit = MediatR.Unit;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class UserPresenceChangedSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly UserPresenceChangedSubscriber _subscriber;

    public UserPresenceChangedSubscriberTests()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RoutePresenceChangeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        _subscriber = new UserPresenceChangedSubscriber(
            _mediatorMock.Object,
            NullLogger<UserPresenceChangedSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ForwardsMappedCommandToMediator()
    {
        RoutePresenceChangeCommand? capturedCommand = null;
        var userId = _fixture.Create<Guid>();
        var recipientUserId = _fixture.Create<Guid>();
        var changedAtUtc = new DateTimeOffset(2026, 3, 17, 10, 15, 0, TimeSpan.Zero);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RoutePresenceChangeCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) =>
                capturedCommand = (RoutePresenceChangeCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        await _subscriber.HandleAsync(
            new PresenceStatusChangedIntegrationEvent
            {
                UserId = userId,
                Status = PresenceStatus.AFK,
                ChangedAtUtc = changedAtUtc,
                RecipientUserIds = [recipientUserId, recipientUserId, Guid.Empty]
            }.ToInboundEnvelope(),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.UserId.Should().Be(userId);
        capturedCommand.Status.Should().Be(PresenceStatus.AFK);
        capturedCommand.ChangedAtUtc.Should().Be(changedAtUtc);
        capturedCommand.RecipientUserIds.Should().ContainSingle().Which.Should().Be(recipientUserId);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIdMissing_ThrowsNonTransientException()
    {
        var act = () => _subscriber.HandleAsync(
            CreateValidEvent(userId: Guid.Empty).ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*UserId*");
        VerifyCommandWasNotSent();
    }

    [Fact]
    public async Task HandleAsync_WhenStatusUnsupported_ThrowsNonTransientException()
    {
        var act = () => _subscriber.HandleAsync(
            CreateValidEvent(status: (PresenceStatus)999).ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*Status*");
        VerifyCommandWasNotSent();
    }

    [Fact]
    public async Task HandleAsync_WhenRecipientUserIdsMissing_ThrowsNonTransientException()
    {
        var act = () => _subscriber.HandleAsync(
            CreateValidEvent(recipientUserIds: [Guid.Empty]).ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*RecipientUserIds*");
        VerifyCommandWasNotSent();
    }

    [Fact]
    public async Task HandleAsync_WhenCommandReturnsFailure_ThrowsNonTransientException()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RoutePresenceChangeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.BadRequest("boom")));

        var act = () => _subscriber.HandleAsync(
            CreateValidEvent().ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("boom");
    }

    private PresenceStatusChangedIntegrationEvent CreateValidEvent(
        Guid? userId = null,
        PresenceStatus status = PresenceStatus.Active,
        IReadOnlyCollection<Guid>? recipientUserIds = null) =>
        new()
        {
            UserId = userId ?? _fixture.Create<Guid>(),
            Status = status,
            ChangedAtUtc = DateTimeOffset.UtcNow,
            RecipientUserIds = (recipientUserIds ?? [_fixture.Create<Guid>()]).ToList()
        };

    private void VerifyCommandWasNotSent()
    {
        _mediatorMock.Verify(
            x => x.Send(It.IsAny<RoutePresenceChangeCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
