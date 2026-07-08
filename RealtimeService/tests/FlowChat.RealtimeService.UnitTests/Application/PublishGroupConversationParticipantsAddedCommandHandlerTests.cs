using AutoFixture;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishGroupConversationParticipantsAdded;
using FluentAssertions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class PublishGroupConversationParticipantsAddedCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeConnectionRegistry> _connectionRegistryMock = new();
    private readonly Mock<IRealtimeGroupManager> _groupManagerMock = new();
    private readonly Mock<IRealtimeClientDispatcher> _dispatcherMock = new();
    private readonly PublishGroupConversationParticipantsAddedCommandHandler _handler;

    public PublishGroupConversationParticipantsAddedCommandHandlerTests()
    {
        _handler = new PublishGroupConversationParticipantsAddedCommandHandler(
            _connectionRegistryMock.Object,
            _groupManagerMock.Object,
            _dispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_JoinsActiveConnectionsToConversationGroupBeforeDispatching()
    {
        var conversationId = _fixture.Create<Guid>();
        var participantUserId = _fixture.Create<Guid>();
        var connectionId = _fixture.Create<string>();
        var callOrder = new List<string>();

        _connectionRegistryMock
            .Setup(x => x.GetConnectionIdsByUserIdsAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(participantUserId)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<string>> { [participantUserId] = [connectionId] });
        _groupManagerMock
            .Setup(x => x.AddToConversationGroupAsync(connectionId, conversationId, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("join"))
            .Returns(Task.CompletedTask);
        _dispatcherMock
            .Setup(x => x.GroupConversationParticipantsAddedAsync(It.IsAny<GroupConversationParticipantsAddedParam>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("dispatch"))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new PublishGroupConversationParticipantsAddedCommand(conversationId, [participantUserId]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        callOrder.Should().Equal("join", "dispatch");
        _groupManagerMock.Verify(
            x => x.AddToConversationGroupAsync(connectionId, conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
