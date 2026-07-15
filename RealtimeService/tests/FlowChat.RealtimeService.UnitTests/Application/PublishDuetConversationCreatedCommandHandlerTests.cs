using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishDuetConversationCreated;
using FluentAssertions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class PublishDuetConversationCreatedCommandHandlerTests
{
    [Fact]
    public async Task Handle_ActiveParticipantConnections_AddsConversationGroupsAndNotifiesClients()
    {
        var conversationId = Guid.NewGuid();
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var connectionRegistry = new CapturingRealtimeConnectionRegistry();
        connectionRegistry.ConnectionIdsByUserId[firstUserId] = ["first-connection"];
        connectionRegistry.ConnectionIdsByUserId[secondUserId] = ["second-connection"];
        var groupManager = new CapturingRealtimeGroupManager();
        var dispatcherMock = new Mock<IRealtimeClientDispatcher>();
        dispatcherMock
            .Setup(x => x.DuetConversationsListChangedAsync(conversationId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new PublishDuetConversationCreatedCommandHandler(
            connectionRegistry,
            groupManager,
            dispatcherMock.Object);

        var result = await handler.Handle(
            new PublishDuetConversationCreatedCommand(conversationId, [firstUserId, secondUserId]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        groupManager.AddedToConversationGroup.Should().BeEquivalentTo(
            [("first-connection", conversationId), ("second-connection", conversationId)]);
        dispatcherMock.Verify(
            x => x.DuetConversationsListChangedAsync(conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
