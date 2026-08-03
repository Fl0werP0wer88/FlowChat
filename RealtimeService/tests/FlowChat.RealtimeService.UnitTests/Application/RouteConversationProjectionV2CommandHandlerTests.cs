using AutoFixture;
using FlowChat.Core.Messaging;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationProjectionV2;
using FlowChat.RealtimeService.Domain.Enums;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RouteConversationProjectionV2CommandHandlerTests
{
    private const int DuetConversationType = 1;
    private const int GroupConversationType = 2;

    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IRealtimeGroupMembershipReadModelRepository> _readModelRepositoryMock = new();
    private readonly Mock<IRealtimeGroupMembershipRevisionTrackerRepository> _revisionTrackerRepositoryMock = new();
    private readonly Mock<IRealtimeEventRouter> _routerMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly RouteConversationProjectionV2CommandHandler _handler;

    public RouteConversationProjectionV2CommandHandlerTests()
    {
        _routerMock
            .Setup(x => x.RouteGroupConversationChangedAsync(
                It.IsAny<GroupConversationChangedParam>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _routerMock
            .Setup(x => x.RouteDuetConversationCreatedAsync(
                It.IsAny<DuetConversationCreatedParam>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(
                (operation, cancellationToken) => operation(cancellationToken));

        _handler = new RouteConversationProjectionV2CommandHandler(
            _readModelRepositoryMock.Object,
            _revisionTrackerRepositoryMock.Object,
            _routerMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_GroupProjectionWithReadyMembership_RoutesGroupConversation()
    {
        var conversationId = _fixture.Create<Guid>();
        var createdByUserId = _fixture.Create<Guid>();
        var participantUserIds = _fixture.CreateMany<Guid>(2).ToArray();
        const string name = "Project group";
        ArrangeReadyMembership(conversationId, participantUserIds);

        var result = await _handler.Handle(
            new RouteConversationProjectionV2Command(
                conversationId,
                conversationId,
                GroupConversationType,
                name,
                createdByUserId,
                OperationType.Created),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _routerMock.Verify(x => x.RouteGroupConversationChangedAsync(
            It.Is<GroupConversationChangedParam>(notification =>
                notification.ConversationId == conversationId &&
                notification.Type == GroupConversationType &&
                notification.Name == name &&
                notification.CreatedByUserId == createdByUserId &&
                notification.ParticipantUserIds.SequenceEqual(participantUserIds)),
            It.IsAny<CancellationToken>()), Times.Once);
        _routerMock.Verify(x => x.RouteDuetConversationCreatedAsync(
            It.IsAny<DuetConversationCreatedParam>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuetProjectionWithReadyMembership_RoutesDuetConversation()
    {
        var conversationId = _fixture.Create<Guid>();
        var participantUserIds = _fixture.CreateMany<Guid>(2).ToArray();
        ArrangeReadyMembership(conversationId, participantUserIds);

        var result = await _handler.Handle(
            new RouteConversationProjectionV2Command(
                conversationId,
                conversationId,
                DuetConversationType,
                null,
                _fixture.Create<Guid>(),
                OperationType.Created),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _routerMock.Verify(x => x.RouteDuetConversationCreatedAsync(
            It.Is<DuetConversationCreatedParam>(notification =>
                notification.ConversationId == conversationId &&
                notification.ParticipantUserIds.SequenceEqual(participantUserIds)),
            It.IsAny<CancellationToken>()), Times.Once);
        _routerMock.Verify(x => x.RouteGroupConversationChangedAsync(
            It.IsAny<GroupConversationChangedParam>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MissingMembershipRevision_ReturnsTransientFailureWithoutRouting()
    {
        var command = CreateGroupCommand();
        _revisionTrackerRepositoryMock
            .Setup(x => x.GetRevisionAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.FailureKind.Should().Be(FailureKind.Transient);
        _readModelRepositoryMock.Verify(x => x.GetUserIdsByResourceIdAsync(
            It.IsAny<RealtimeGroupType>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
        VerifyNoRouting();
    }

    [Fact]
    public async Task Handle_EmptyMembershipProjection_ReturnsTransientFailureWithoutRouting()
    {
        var command = CreateGroupCommand();
        _revisionTrackerRepositoryMock
            .Setup(x => x.GetRevisionAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _readModelRepositoryMock
            .Setup(x => x.GetUserIdsByResourceIdAsync(
                RealtimeGroupType.Conversation,
                command.ConversationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.FailureKind.Should().Be(FailureKind.Transient);
        VerifyNoRouting();
    }

    private void ArrangeReadyMembership(Guid conversationId, IReadOnlyList<Guid> participantUserIds)
    {
        _revisionTrackerRepositoryMock
            .Setup(x => x.GetRevisionAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _readModelRepositoryMock
            .Setup(x => x.GetUserIdsByResourceIdAsync(
                RealtimeGroupType.Conversation,
                conversationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(participantUserIds);
    }

    private RouteConversationProjectionV2Command CreateGroupCommand()
    {
        var conversationId = _fixture.Create<Guid>();
        return new RouteConversationProjectionV2Command(
            conversationId,
            conversationId,
            GroupConversationType,
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            OperationType.Created);
    }

    private void VerifyNoRouting()
    {
        _routerMock.Verify(x => x.RouteGroupConversationChangedAsync(
            It.IsAny<GroupConversationChangedParam>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _routerMock.Verify(x => x.RouteDuetConversationCreatedAsync(
            It.IsAny<DuetConversationCreatedParam>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
