using AutoFixture;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationChanged;
using FluentAssertions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RouteConversationChangedCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeEventRouter> _routerMock = new();
    private readonly RouteConversationChangedCommandHandler _handler;

    public RouteConversationChangedCommandHandlerTests()
    {
        _routerMock
            .Setup(x => x.RouteConversationChangedAsync(It.IsAny<ConversationChangedParam>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RouteConversationChangedCommandHandler(_routerMock.Object);
    }

    [Fact]
    public async Task Handle_NormalizesParticipantsAndRoutes()
    {
        ConversationChangedParam? capturedNotification = null;
        var participantUserId = _fixture.Create<Guid>();

        _routerMock
            .Setup(x => x.RouteConversationChangedAsync(It.IsAny<ConversationChangedParam>(), It.IsAny<CancellationToken>()))
            .Callback<ConversationChangedParam, CancellationToken>((notification, _) => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new RouteConversationChangedCommand(
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
