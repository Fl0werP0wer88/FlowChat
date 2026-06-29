using AutoFixture;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationChanged;
using FluentAssertions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RouteGroupConversationChangedCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeEventRouter> _routerMock = new();
    private readonly RouteGroupConversationChangedCommandHandler _handler;

    public RouteGroupConversationChangedCommandHandlerTests()
    {
        _routerMock
            .Setup(x => x.RouteGroupConversationChangedAsync(It.IsAny<GroupConversationChangedParam>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RouteGroupConversationChangedCommandHandler(_routerMock.Object);
    }

    [Fact]
    public async Task Handle_NormalizesParticipantsAndRoutes()
    {
        GroupConversationChangedParam? capturedNotification = null;
        var participantUserId = _fixture.Create<Guid>();

        _routerMock
            .Setup(x => x.RouteGroupConversationChangedAsync(It.IsAny<GroupConversationChangedParam>(), It.IsAny<CancellationToken>()))
            .Callback<GroupConversationChangedParam, CancellationToken>((notification, _) => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new RouteGroupConversationChangedCommand(
                _fixture.Create<Guid>(),
                1,
                null,
                _fixture.Create<Guid>(),
                [participantUserId, participantUserId, Guid.Empty]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedNotification.Should().NotBeNull();
        capturedNotification!.ParticipantUserIds.Should().ContainSingle().Which.Should().Be(participantUserId);
    }
}
