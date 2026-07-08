using AutoFixture;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationParticipantsRemoved;
using FlowChat.RealtimeService.Domain.Enums;
using FluentAssertions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RouteGroupConversationParticipantsRemovedCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeGroupMembershipReadModelRepository> _readModelRepositoryMock = new();
    private readonly Mock<IRealtimeEventRouter> _routerMock = new();
    private readonly RouteGroupConversationParticipantsRemovedCommandHandler _handler;

    public RouteGroupConversationParticipantsRemovedCommandHandlerTests()
    {
        _routerMock
            .Setup(x => x.RouteGroupConversationParticipantsRemovedAsync(It.IsAny<GroupConversationParticipantsRemovedParam>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RouteGroupConversationParticipantsRemovedCommandHandler(_readModelRepositoryMock.Object, _routerMock.Object);
    }

    [Fact]
    public async Task Handle_NormalizesParticipants_RoutesAndRemovesReadModel()
    {
        var conversationId = _fixture.Create<Guid>();
        var participantUserId = _fixture.Create<Guid>();
        GroupConversationParticipantsRemovedParam? capturedNotification = null;

        _routerMock
            .Setup(x => x.RouteGroupConversationParticipantsRemovedAsync(It.IsAny<GroupConversationParticipantsRemovedParam>(), It.IsAny<CancellationToken>()))
            .Callback<GroupConversationParticipantsRemovedParam, CancellationToken>((notification, _) => capturedNotification = notification)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new RouteGroupConversationParticipantsRemovedCommand(conversationId, [participantUserId, participantUserId, Guid.Empty]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _readModelRepositoryMock.Verify(
            x => x.RemoveRangeAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { participantUserId })),
                RealtimeGroupType.Conversation,
                conversationId,
                It.IsAny<CancellationToken>()),
            Times.Once);
        capturedNotification.Should().NotBeNull();
        capturedNotification!.ParticipantUserIds.Should().ContainSingle().Which.Should().Be(participantUserId);
    }
}
