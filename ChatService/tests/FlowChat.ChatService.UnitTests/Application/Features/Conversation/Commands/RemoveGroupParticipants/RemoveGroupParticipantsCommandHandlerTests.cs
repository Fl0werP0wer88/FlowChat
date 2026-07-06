using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Commands.RemoveGroupParticipants;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Commands.RemoveGroupParticipants;

public sealed class RemoveGroupParticipantsCommandHandlerTests
{
    private readonly Mock<IGroupConversationWriteRepository> _conversationRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILocalEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IAggregateBeforeSaveProcessor<RemoveGroupParticipantsCommand, GroupConversation>> _beforeSaveProcessorMock = new();
    private readonly RemoveGroupParticipantsCommandHandler _handler;

    public RemoveGroupParticipantsCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _beforeSaveProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<RemoveGroupParticipantsCommand>(),
                It.IsAny<GroupConversation>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RemoveGroupParticipantsCommandHandler(
            _conversationRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            [_beforeSaveProcessorMock.Object]);
    }

    [Fact]
    public async Task Handle_ValidRequest_RemovesParticipant()
    {
        var creatorId = Guid.NewGuid();
        var memberToRemoveId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new RemoveGroupParticipantsCommand(conversationId, [memberToRemoveId]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            creatorId,
            [creatorId, memberToRemoveId, Guid.NewGuid()],
            "Dev Team");
        conversation.ClearEvents();

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
        conversation.Participants.Should().NotContain(p => p.UserId.Value == memberToRemoveId);
    }

    [Fact]
    public async Task Handle_MultipleParticipants_RemovesAll()
    {
        var creatorId = Guid.NewGuid();
        var memberToRemoveId1 = Guid.NewGuid();
        var memberToRemoveId2 = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new RemoveGroupParticipantsCommand(conversationId, [memberToRemoveId1, memberToRemoveId2]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            creatorId,
            [creatorId, memberToRemoveId1, memberToRemoveId2, Guid.NewGuid()],
            "Dev Team");
        conversation.ClearEvents();

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
        conversation.Participants.Should().NotContain(p => p.UserId.Value == memberToRemoveId1);
        conversation.Participants.Should().NotContain(p => p.UserId.Value == memberToRemoveId2);
    }

    [Fact]
    public async Task Handle_WhenParticipantIsNotInConversation_ReturnsBadRequest()
    {
        var creatorId = Guid.NewGuid();
        var existingMemberId = Guid.NewGuid();
        var notAParticipantId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new RemoveGroupParticipantsCommand(conversationId, [notAParticipantId]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            creatorId,
            [creatorId, existingMemberId],
            "Dev Team");
        conversation.ClearEvents();

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<RemoveGroupParticipantsCommand>(),
                It.IsAny<GroupConversation>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRemovalWouldDropBelowMinimumParticipants_ReturnsBadRequest()
    {
        var creatorId = Guid.NewGuid();
        var memberToRemoveId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new RemoveGroupParticipantsCommand(conversationId, [memberToRemoveId]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            creatorId,
            [creatorId, memberToRemoveId],
            "Dev Team");
        conversation.ClearEvents();

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
    }

    [Fact]
    public async Task Handle_WhenGroupConversationNotFound_ReturnsNotFound()
    {
        var conversationId = Guid.NewGuid();
        var command = new RemoveGroupParticipantsCommand(conversationId, [Guid.NewGuid()]);

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GroupConversation?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ValidRequest_DispatchesDomainEvents()
    {
        var creatorId = Guid.NewGuid();
        var memberToRemoveId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var command = new RemoveGroupParticipantsCommand(conversationId, [memberToRemoveId]);

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            creatorId,
            [creatorId, memberToRemoveId, Guid.NewGuid()],
            "Dev Team");
        conversation.ClearEvents();

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<RemoveGroupParticipantsCommand>(),
                It.IsAny<GroupConversation>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
