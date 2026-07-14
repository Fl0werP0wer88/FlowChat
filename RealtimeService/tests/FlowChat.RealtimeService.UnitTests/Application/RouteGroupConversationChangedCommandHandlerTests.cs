using AutoFixture;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationChanged;
using FlowChat.RealtimeService.Domain.Enums;
using FluentAssertions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RouteGroupConversationChangedCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeEventRouter> _routerMock = new();
    private readonly Mock<IRealtimeGroupMembershipReadModelRepository> _readModelRepositoryMock = new();
    private readonly RouteGroupConversationChangedCommandHandler _handler;

    public RouteGroupConversationChangedCommandHandlerTests()
    {
        _routerMock
            .Setup(x => x.RouteGroupConversationChangedAsync(It.IsAny<GroupConversationChangedParam>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RouteGroupConversationChangedCommandHandler(_routerMock.Object, _readModelRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ResolvesParticipantsFromReadModelAndRoutes()
    {
        GroupConversationChangedParam? capturedNotification = null;
        var conversationId = _fixture.Create<Guid>();
        var participantUserId = _fixture.Create<Guid>();

        _readModelRepositoryMock
            .Setup(x => x.GetUserIdsByResourceIdAsync(RealtimeGroupType.Conversation, conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([participantUserId]);
        _routerMock
            .Setup(x => x.RouteGroupConversationChangedAsync(It.IsAny<GroupConversationChangedParam>(), It.IsAny<CancellationToken>()))
            .Callback<GroupConversationChangedParam, CancellationToken>((notification, _) => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new RouteGroupConversationChangedCommand(
                conversationId,
                1,
                null,
                _fixture.Create<Guid>()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedNotification.Should().NotBeNull();
        capturedNotification!.ParticipantUserIds.Should().ContainSingle().Which.Should().Be(participantUserId);
    }
}
