using AutoFixture;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishConversationParticipantsRemoved;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class PublishConversationParticipantsRemovedCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeConnectionRegistry> _connectionRegistryMock = new();
    private readonly Mock<IRealtimeGroupManager> _groupManagerMock = new();
    private readonly Mock<IRealtimeClientDispatcher> _dispatcherMock = new();
    private readonly PublishConversationParticipantsRemovedCommandHandler _handler;

    public PublishConversationParticipantsRemovedCommandHandlerTests()
    {
        _handler = new PublishConversationParticipantsRemovedCommandHandler(
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
        const int conversationType = 2;
        const int participantCount = 3;
        const int membershipRevision = 8;
        var callOrder = new List<string>();

        _connectionRegistryMock
            .Setup(x => x.GetConnectionIdsByUserIdsAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(participantUserId)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<string>> { [participantUserId] = [connectionId] });
        _dispatcherMock
            .Setup(x => x.ConversationParticipantsRemovedAsync(
                It.Is<ConversationParticipantsRemovedParam>(notification =>
                    notification.ConversationId == conversationId &&
                    notification.ConversationType == conversationType &&
                    notification.ParticipantCount == participantCount &&
                    notification.MembershipRevision == membershipRevision &&
                    notification.ParticipantUserIds.SequenceEqual(new[] { participantUserId })),
                It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("dispatch"))
            .Returns(Task.CompletedTask);
        _groupManagerMock
            .Setup(x => x.RemoveFromConversationGroupAsync(connectionId, conversationId, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("leave"))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new PublishConversationParticipantsRemovedCommand(
                conversationId,
                conversationType,
                [participantUserId],
                participantCount,
                membershipRevision),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
        callOrder.Should().Equal("dispatch", "leave");
        _groupManagerMock.Verify(
            x => x.RemoveFromConversationGroupAsync(connectionId, conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
