using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Commands.AddParticipant;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;


namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Commands.AddParticipant;

public sealed class AddParticipantCommandHandlerTests
{
    private readonly Mock<IGroupConversationWriteRepository> _conversationRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IDbUpdateExceptionClassifier> _dbUpdateExceptionClassifierMock = new();
    private readonly AddParticipantCommandHandler _handler;

    public AddParticipantCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<bool>>>>>(),
                It.IsAny<Func<FlowChatResult<IdempotentCommandResult<bool>>, CancellationToken, Task<FlowChatResult<IdempotentCommandResult<bool>>>>>(),
                It.IsAny<Func<Exception, CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<
                Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<bool>>>>,
                Func<FlowChatResult<IdempotentCommandResult<bool>>, CancellationToken, Task<FlowChatResult<IdempotentCommandResult<bool>>>>,
                Func<Exception, CancellationToken, Task>,
                CancellationToken>(async (operation, beforeCommitOperation, _, ct) =>
                {
                    var result = await operation(ct);
                    return await beforeCommitOperation(result, ct);
                });

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new AddParticipantCommandHandler(
            _conversationRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            _dbUpdateExceptionClassifierMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_AddsParticipantAndDispatchesEvents()
    {
        var creatorId = Guid.NewGuid();
        var existingMemberId = Guid.NewGuid();
        var newMemberId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new AddParticipantCommand(conversationId, [newMemberId]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            creatorId,
            [creatorId, existingMemberId],
            "Dev Team");
        conversation.ClearEvents();

        List<IDomainEvent> dispatchedEvents = [];

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeTrue();
        result.Value.WasAlreadyProcessed.Should().BeFalse();
        conversation.Participants.Should().Contain(p => p.UserId == newMemberId);
        dispatchedEvents.OfType<ParticipantAddedDomainEvent>().Should().ContainSingle()
            .Which.ParticipantUserId.Should().Be(newMemberId);
        _conversationRepositoryMock.Verify(x => x.UpdateAsync(conversation, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MultipleNewParticipants_AddsAllAndDispatchesEventsForEach()
    {
        var creatorId = Guid.NewGuid();
        var newMemberId1 = Guid.NewGuid();
        var newMemberId2 = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new AddParticipantCommand(conversationId, [newMemberId1, newMemberId2]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            creatorId,
            [creatorId, Guid.NewGuid()],
            "Dev Team");
        conversation.ClearEvents();

        List<IDomainEvent> dispatchedEvents = [];

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeTrue();
        conversation.Participants.Should().Contain(p => p.UserId == newMemberId1);
        conversation.Participants.Should().Contain(p => p.UserId == newMemberId2);
        dispatchedEvents.OfType<ParticipantAddedDomainEvent>().Should().HaveCount(2);
        _conversationRepositoryMock.Verify(x => x.UpdateAsync(conversation, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MixedParticipants_AddsOnlyNewOnesAndReturnsTrue()
    {
        var creatorId = Guid.NewGuid();
        var existingMemberId = Guid.NewGuid();
        var newMemberId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new AddParticipantCommand(conversationId, [existingMemberId, newMemberId]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            creatorId,
            [creatorId, existingMemberId],
            "Dev Team");
        conversation.ClearEvents();

        List<IDomainEvent> dispatchedEvents = [];

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeTrue();
        dispatchedEvents.OfType<ParticipantAddedDomainEvent>().Should().ContainSingle()
            .Which.ParticipantUserId.Should().Be(newMemberId);
        _conversationRepositoryMock.Verify(x => x.UpdateAsync(conversation, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAllParticipantsAlreadyInConversation_ReturnsSuccessWithoutDispatchingEvents()
    {
        var creatorId = Guid.NewGuid();
        var existingMemberId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new AddParticipantCommand(conversationId, [existingMemberId]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            creatorId,
            [creatorId, existingMemberId],
            "Dev Team");
        conversation.ClearEvents();

        List<IDomainEvent> dispatchedEvents = [];

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeFalse();
        result.Value.WasAlreadyProcessed.Should().BeFalse();
        dispatchedEvents.Should().BeEmpty();
        _conversationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<GroupConversation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRaceConditionCausesDbConflict_ReturnsAlreadyProcessed()
    {
        var conversationId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var command = new AddParticipantCommand(conversationId, [participantId]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            Guid.NewGuid(),
            [Guid.NewGuid(), Guid.NewGuid()],
            "Dev Team");
        conversation.ClearEvents();

        List<IDomainEvent> dispatchedEvents = [];

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _conversationRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<GroupConversation>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("duplicate"));
        _dbUpdateExceptionClassifierMock
            .Setup(x => x.IsExpectedIdempotencyConflict(It.IsAny<DbUpdateException>(), AddParticipantCommand.IdempotencyConflictKey))
            .Returns(true);
        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeFalse();
        result.Value.WasAlreadyProcessed.Should().BeTrue();
        dispatchedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenGroupConversationNotFound_ReturnsNotFound()
    {
        var conversationId = Guid.NewGuid();
        var command = new AddParticipantCommand(conversationId, [Guid.NewGuid()]);

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GroupConversation?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }
}

