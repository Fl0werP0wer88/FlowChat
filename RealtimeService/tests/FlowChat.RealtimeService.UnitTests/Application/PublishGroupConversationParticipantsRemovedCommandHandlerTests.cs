using AutoFixture;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishGroupConversationParticipantsRemoved;
using FluentAssertions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class PublishGroupConversationParticipantsRemovedCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeConnectionRegistry> _connectionRegistryMock = new();
    private readonly Mock<IRealtimeGroupManager> _groupManagerMock = new();
    private readonly Mock<IRealtimeClientDispatcher> _dispatcherMock = new();
    private readonly PublishGroupConversationParticipantsRemovedCommandHandler _handler;

    public PublishGroupConversationParticipantsRemovedCommandHandlerTests()
    {
        _handler = new PublishGroupConversationParticipantsRemovedCommandHandler(
            _connectionRegistryMock.Object,
            _groupManagerMock.Object,
            _dispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_DispatchesBeforeRemovingActiveConnectionsFromConversationGroup()
    {
        var conversationId = _fixture.Create<Guid>();
        var participantUserId = _fixture.Create<Guid>();
        var connectionId = _fixture.Create<string>();
        var callOrder = new List<string>();

        _connectionRegistryMock
            .Setup(x => x.GetConnectionIdsByUserIdAsync(participantUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([connectionId]);
        _dispatcherMock
            .Setup(x => x.GroupConversationParticipantsRemovedAsync(It.IsAny<GroupConversationParticipantsRemovedParam>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("dispatch"))
            .Returns(Task.CompletedTask);
        _groupManagerMock
            .Setup(x => x.RemoveFromConversationGroupAsync(connectionId, conversationId, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("leave"))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new PublishGroupConversationParticipantsRemovedCommand(conversationId, [participantUserId]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        callOrder.Should().Equal("dispatch", "leave");
        _groupManagerMock.Verify(
            x => x.RemoveFromConversationGroupAsync(connectionId, conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
