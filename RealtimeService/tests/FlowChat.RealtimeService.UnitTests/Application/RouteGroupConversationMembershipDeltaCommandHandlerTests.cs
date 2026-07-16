using AutoFixture;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationMembershipDelta;
using FlowChat.RealtimeService.Domain.Enums;
using FlowChat.Shared.Application;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RouteGroupConversationMembershipDeltaCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeGroupMembershipReadModelRepository> _readModelRepositoryMock = new();
    private readonly Mock<IRealtimeGroupMembershipRevisionTrackerRepository> _revisionTrackerRepositoryMock = new();
    private readonly Mock<IRealtimeEventRouter> _routerMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly RouteGroupConversationMembershipDeltaCommandHandler _handler;

    public RouteGroupConversationMembershipDeltaCommandHandlerTests()
    {
        _routerMock
            .Setup(x => x.RouteGroupConversationParticipantsAddedAsync(It.IsAny<GroupConversationParticipantsAddedParam>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _routerMock
            .Setup(x => x.RouteGroupConversationParticipantsRemovedAsync(It.IsAny<GroupConversationParticipantsRemovedParam>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _handler = new RouteGroupConversationMembershipDeltaCommandHandler(
            _readModelRepositoryMock.Object,
            _revisionTrackerRepositoryMock.Object,
            _routerMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_AddedDelta_PersistsRevisionAndRoutesAddedNotification()
    {
        var conversationId = _fixture.Create<Guid>();
        var participantUserId = _fixture.Create<Guid>();
        const int revision = 2;

        var result = await _handler.Handle(
            new RouteGroupConversationMembershipDeltaCommand(
                conversationId,
                [participantUserId, participantUserId, Guid.Empty],
                DeltaOperationType.Added,
                revision),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _readModelRepositoryMock.Verify(x => x.AddRangeAsync(
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { participantUserId })),
            RealtimeGroupType.Conversation,
            conversationId,
            It.IsAny<CancellationToken>()), Times.Once);
        _revisionTrackerRepositoryMock.Verify(
            x => x.UpsertIfNewerAsync(conversationId, revision, It.IsAny<CancellationToken>()),
            Times.Once);
        _routerMock.Verify(x => x.RouteGroupConversationParticipantsAddedAsync(
            It.Is<GroupConversationParticipantsAddedParam>(notification =>
                notification.ConversationId == conversationId &&
                notification.ParticipantUserIds.SequenceEqual(new[] { participantUserId })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RemovedDelta_RemovesMembershipsAndRoutesRemovedNotification()
    {
        var conversationId = _fixture.Create<Guid>();
        var participantUserId = _fixture.Create<Guid>();
        const int revision = 3;

        var result = await _handler.Handle(
            new RouteGroupConversationMembershipDeltaCommand(
                conversationId,
                [participantUserId],
                DeltaOperationType.Removed,
                revision),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _readModelRepositoryMock.Verify(x => x.RemoveRangeAsync(
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { participantUserId })),
            RealtimeGroupType.Conversation,
            conversationId,
            It.IsAny<CancellationToken>()), Times.Once);
        _routerMock.Verify(x => x.RouteGroupConversationParticipantsRemovedAsync(
            It.Is<GroupConversationParticipantsRemovedParam>(notification =>
                notification.ConversationId == conversationId &&
                notification.ParticipantUserIds.SequenceEqual(new[] { participantUserId })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    public async Task Handle_StaleOrDuplicateRevision_DoesNotPersistOrRoute(int trackedRevision)
    {
        var conversationId = _fixture.Create<Guid>();
        _revisionTrackerRepositoryMock
            .Setup(x => x.GetRevisionAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackedRevision);

        var result = await _handler.Handle(
            new RouteGroupConversationMembershipDeltaCommand(
                conversationId,
                [_fixture.Create<Guid>()],
                DeltaOperationType.Added,
                4),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _readModelRepositoryMock.VerifyNoOtherCalls();
        _routerMock.VerifyNoOtherCalls();
        _revisionTrackerRepositoryMock.Verify(
            x => x.GetRevisionAsync(conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
