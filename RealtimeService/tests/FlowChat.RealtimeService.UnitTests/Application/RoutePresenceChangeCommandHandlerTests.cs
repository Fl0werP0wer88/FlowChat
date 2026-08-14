using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Features.Presence.Commands.RoutePresenceChange;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RoutePresenceChangeCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeEventRouter> _routerMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly RoutePresenceChangeCommandHandler _handler;

    public RoutePresenceChangeCommandHandlerTests()
    {
        _routerMock
            .Setup(x => x.RoutePresenceChangeAsync(It.IsAny<PresenceChangedParam>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(
                (operation, cancellationToken) => operation(cancellationToken));

        _handler = new RoutePresenceChangeCommandHandler(
            _routerMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_NormalizesStatusAndRoutes()
    {
        PresenceChangedParam? capturedNotification = null;
        var recipientUserId = _fixture.Create<Guid>();

        _routerMock
            .Setup(x => x.RoutePresenceChangeAsync(It.IsAny<PresenceChangedParam>(), It.IsAny<CancellationToken>()))
            .Callback<PresenceChangedParam, CancellationToken>((notification, _) => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new RoutePresenceChangeCommand(
                _fixture.Create<Guid>(),
                PresenceStatus.Active,
                new DateTimeOffset(2026, 3, 17, 12, 30, 0, TimeSpan.Zero),
                [recipientUserId]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Status.Should().Be(PresenceStatus.Active);
        capturedNotification.RecipientUserIds.Should().ContainSingle().Which.Should().Be(recipientUserId);
    }
}
