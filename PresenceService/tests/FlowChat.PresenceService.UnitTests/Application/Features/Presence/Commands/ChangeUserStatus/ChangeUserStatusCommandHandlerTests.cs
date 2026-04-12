using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.Presence;
using FlowChat.PresenceService.Application.Features.Presence.Commands.ChangeUserStatus;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class ChangeUserStatusCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IContactObserverProjectionReadRepository> _readRepositoryMock = new();
    private readonly Mock<IPresenceStatusStore> _presenceStatusStoreMock = new();
    private readonly Mock<IPresenceStatusUpdateService> _presenceStatusUpdateServiceMock = new();
    private readonly ChangeUserStatusCommandHandler _handler;

    public ChangeUserStatusCommandHandlerTests()
    {
        _presenceStatusUpdateServiceMock
            .Setup(x => x.UpdateAndPublishAsync(
                It.IsAny<Guid>(),
                It.IsAny<PresenceStatusSnapshot?>(),
                It.IsAny<UserStatusChangedIntegrationEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        _handler = new ChangeUserStatusCommandHandler(
            _readRepositoryMock.Object,
            _presenceStatusStoreMock.Object,
            _presenceStatusUpdateServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenStatusChanges_PublishesDistinctRecipientEvent()
    {
        var userId = _fixture.Create<Guid>();
        var recipient1 = _fixture.Create<Guid>();
        var recipient2 = _fixture.Create<Guid>();
        var previous = new PresenceStatusSnapshot(userId, UserStatus.Active, DateTimeOffset.UtcNow.AddMinutes(-5));
        UserStatusChangedIntegrationEvent? capturedEvent = null;

        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previous);
        _readRepositoryMock
            .Setup(x => x.GetObserverUserIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([recipient1, recipient1, Guid.Empty, recipient2]);
        _presenceStatusUpdateServiceMock
            .Setup(x => x.UpdateAndPublishAsync(
                userId,
                previous,
                It.IsAny<UserStatusChangedIntegrationEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, PresenceStatusSnapshot?, UserStatusChangedIntegrationEvent, CancellationToken>((_, _, integrationEvent, _) => capturedEvent = integrationEvent)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        var result = await _handler.Handle(
            new ChangeUserStatusCommand(userId, UserStatus.Busy),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Key.Should().Be(userId.ToString("D"));
        capturedEvent.UserId.Should().Be(userId);
        capturedEvent.Status.Should().Be(UserStatus.Busy);
        capturedEvent.RecipientUserIds.Should().BeEquivalentTo([recipient1, recipient2]);
        capturedEvent.ChangedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_WhenStatusIsUnchanged_ReturnsSuccessWithoutPublishing()
    {
        var userId = _fixture.Create<Guid>();
        _presenceStatusStoreMock
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PresenceStatusSnapshot(userId, UserStatus.Invisible, DateTimeOffset.UtcNow));

        var result = await _handler.Handle(
            new ChangeUserStatusCommand(userId, UserStatus.Invisible),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _readRepositoryMock.Verify(
            x => x.GetObserverUserIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _presenceStatusUpdateServiceMock.Verify(
            x => x.UpdateAndPublishAsync(
                It.IsAny<Guid>(),
                It.IsAny<PresenceStatusSnapshot?>(),
                It.IsAny<UserStatusChangedIntegrationEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
