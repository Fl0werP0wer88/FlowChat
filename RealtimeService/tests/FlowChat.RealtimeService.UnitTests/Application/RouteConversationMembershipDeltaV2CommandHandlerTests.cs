using AutoFixture;
using FlowChat.Core.Messaging;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationMembershipDeltaV2;
using FlowChat.RealtimeService.Domain.Enums;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RouteConversationMembershipDeltaV2CommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeGroupMembershipReadModelRepository> _readModelRepositoryMock = new();
    private readonly Mock<IRealtimeGroupMembershipRevisionTrackerRepository> _revisionTrackerRepositoryMock = new();
    private readonly Mock<IRealtimeEventRouter> _routerMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly RouteConversationMembershipDeltaV2CommandHandler _handler;

    public RouteConversationMembershipDeltaV2CommandHandlerTests()
    {
        _routerMock
            .Setup(x => x.RouteGroupConversationParticipantsAddedAsync(
                It.IsAny<GroupConversationParticipantsAddedParam>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _routerMock
            .Setup(x => x.RouteGroupConversationParticipantsRemovedAsync(
                It.IsAny<GroupConversationParticipantsRemovedParam>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(
                (operation, cancellationToken) => operation(cancellationToken));

        _handler = new RouteConversationMembershipDeltaV2CommandHandler(
            _readModelRepositoryMock.Object,
            _revisionTrackerRepositoryMock.Object,
            _routerMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_FirstRevisionTwo_PersistsInitialMembershipWithoutNotifications()
    {
        var conversationId = _fixture.Create<Guid>();
        var participantUserIds = _fixture.CreateMany<Guid>(2).ToArray();
        _revisionTrackerRepositoryMock
            .Setup(x => x.GetRevisionAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var result = await _handler.Handle(
            new RouteConversationMembershipDeltaV2Command(
                conversationId,
                2,
                participantUserIds
                    .Select(userId => new ConversationMembershipDeltaItemV2(userId, OperationType.Created))
                    .ToArray()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _readModelRepositoryMock.Verify(x => x.AddRangeAsync(
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(participantUserIds)),
            RealtimeGroupType.Conversation,
            conversationId,
            It.IsAny<CancellationToken>()), Times.Once);
        _revisionTrackerRepositoryMock.Verify(
            x => x.UpsertIfNewerAsync(conversationId, 2, It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyNoNotifications();
    }

    [Fact]
    public async Task Handle_NextRevisionWithMixedDelta_AppliesAllChangesAndRoutesBothNotifications()
    {
        var conversationId = _fixture.Create<Guid>();
        var addedParticipantUserIds = _fixture.CreateMany<Guid>(2).ToArray();
        var removedParticipantUserIds = _fixture.CreateMany<Guid>(2).ToArray();
        _revisionTrackerRepositoryMock
            .Setup(x => x.GetRevisionAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var delta = addedParticipantUserIds
            .Select(userId => new ConversationMembershipDeltaItemV2(userId, OperationType.Created))
            .Concat(removedParticipantUserIds.Select(
                userId => new ConversationMembershipDeltaItemV2(userId, OperationType.Deleted)))
            .ToArray();

        var result = await _handler.Handle(
            new RouteConversationMembershipDeltaV2Command(conversationId, 3, delta),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _readModelRepositoryMock.Verify(x => x.AddRangeAsync(
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(addedParticipantUserIds)),
            RealtimeGroupType.Conversation,
            conversationId,
            It.IsAny<CancellationToken>()), Times.Once);
        _readModelRepositoryMock.Verify(x => x.RemoveRangeAsync(
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(removedParticipantUserIds)),
            RealtimeGroupType.Conversation,
            conversationId,
            It.IsAny<CancellationToken>()), Times.Once);
        _revisionTrackerRepositoryMock.Verify(
            x => x.UpsertIfNewerAsync(conversationId, 3, It.IsAny<CancellationToken>()),
            Times.Once);
        _routerMock.Verify(x => x.RouteGroupConversationParticipantsAddedAsync(
            It.Is<GroupConversationParticipantsAddedParam>(notification =>
                notification.ConversationId == conversationId &&
                notification.ParticipantUserIds.SequenceEqual(addedParticipantUserIds)),
            It.IsAny<CancellationToken>()), Times.Once);
        _routerMock.Verify(x => x.RouteGroupConversationParticipantsRemovedAsync(
            It.Is<GroupConversationParticipantsRemovedParam>(notification =>
                notification.ConversationId == conversationId &&
                notification.ParticipantUserIds.SequenceEqual(removedParticipantUserIds)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Handle_DuplicateOrOlderRevision_ReturnsSuccessWithoutSideEffects(int incomingRevision)
    {
        var conversationId = _fixture.Create<Guid>();
        _revisionTrackerRepositoryMock
            .Setup(x => x.GetRevisionAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var result = await _handler.Handle(
            CreateAddCommand(conversationId, incomingRevision),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        VerifyNoProjectionMutations(conversationId);
        VerifyNoNotifications();
    }

    [Theory]
    [InlineData(null, 3)]
    [InlineData(2, 4)]
    public async Task Handle_RevisionGap_ReturnsTransientFailureWithoutSideEffects(
        int? trackedRevision,
        int incomingRevision)
    {
        var conversationId = _fixture.Create<Guid>();
        _revisionTrackerRepositoryMock
            .Setup(x => x.GetRevisionAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackedRevision);

        var result = await _handler.Handle(
            CreateAddCommand(conversationId, incomingRevision),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.FailureKind.Should().Be(FailureKind.Transient);
        VerifyNoProjectionMutations(conversationId);
        VerifyNoNotifications();
    }

    private RouteConversationMembershipDeltaV2Command CreateAddCommand(
        Guid conversationId,
        int projectionRevision) =>
        new(
            conversationId,
            projectionRevision,
            [new ConversationMembershipDeltaItemV2(_fixture.Create<Guid>(), OperationType.Created)]);

    private void VerifyNoProjectionMutations(Guid conversationId)
    {
        _readModelRepositoryMock.Verify(x => x.AddRangeAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(),
            It.IsAny<RealtimeGroupType>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _readModelRepositoryMock.Verify(x => x.RemoveRangeAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(),
            It.IsAny<RealtimeGroupType>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _revisionTrackerRepositoryMock.Verify(
            x => x.UpsertIfNewerAsync(conversationId, It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void VerifyNoNotifications()
    {
        _routerMock.Verify(x => x.RouteGroupConversationParticipantsAddedAsync(
            It.IsAny<GroupConversationParticipantsAddedParam>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _routerMock.Verify(x => x.RouteGroupConversationParticipantsRemovedAsync(
            It.IsAny<GroupConversationParticipantsRemovedParam>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
