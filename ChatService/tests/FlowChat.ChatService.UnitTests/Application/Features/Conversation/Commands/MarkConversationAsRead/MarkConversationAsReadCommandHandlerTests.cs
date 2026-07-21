using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Commands.MarkConversationAsRead;
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

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Commands.MarkConversationAsRead;

public sealed class MarkConversationAsReadCommandHandlerTests
{
    private readonly Mock<IConversationWriteRepository> _conversationRepositoryMock = new();
    private readonly Mock<IChatMessageReadRepository> _chatMessageRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILocalEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IAggregateBeforeSaveProcessorV2<MarkConversationAsReadCommand, ConversationAggregate>> _beforeSaveProcessorMock = new();
    private readonly MarkConversationAsReadCommandHandler _handler;

    public MarkConversationAsReadCommandHandlerTests()
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
                It.IsAny<MarkConversationAsReadCommand>(),
                It.IsAny<ConversationAggregate>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new MarkConversationAsReadCommandHandler(
            _conversationRepositoryMock.Object,
            _chatMessageRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            [_beforeSaveProcessorMock.Object]);
    }

    [Fact]
    public async Task Handle_WhenParticipantUnread_MarksOnlyCallerAsRead()
    {
        var callerId = Guid.NewGuid();
        var otherParticipantId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var conversation = CreateGroupConversation(conversationId, callerId, otherParticipantId);
        conversation.ClearEvents();
        var command = new MarkConversationAsReadCommand(conversationId, callerId);

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _chatMessageRepositoryMock
            .Setup(x => x.GetMaxSequenceNumAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        conversation.Participants.Should().ContainSingle(p =>
            p.UserId.Value == callerId &&
            p.LastReadMessageSequenceNum == 42);
        conversation.Participants.Should().ContainSingle(p =>
            p.UserId.Value == otherParticipantId &&
            p.LastReadMessageSequenceNum == 0);
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(command, conversation, MutationType.Updated, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenReadStateAlreadyCurrent_ReturnsSuccessWithoutProcessingAggregateChanges()
    {
        var callerId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var conversation = CreateGroupConversation(conversationId, callerId, Guid.NewGuid());
        conversation.MarkParticipantAsRead(callerId, 12);
        conversation.ClearEvents();
        var initialVersion = conversation.Version;
        var command = new MarkConversationAsReadCommand(conversationId, callerId);

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _chatMessageRepositoryMock
            .Setup(x => x.GetMaxSequenceNumAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        conversation.Version.Should().Be(initialVersion);
        _domainEventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<MarkConversationAsReadCommand>(),
                It.IsAny<ConversationAggregate>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenConversationIsMissing_ReturnsNotFound()
    {
        var command = new MarkConversationAsReadCommand(Guid.NewGuid(), Guid.NewGuid());

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConversationAggregate?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenConversationIsDuet_MarksCallerAsRead()
    {
        var callerId = Guid.NewGuid();
        var otherParticipantId = Guid.NewGuid();
        var conversation = DuetConversation.Create(callerId, otherParticipantId);
        conversation.ClearEvents();
        var command = new MarkConversationAsReadCommand(conversation.Id.Value, callerId);

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _chatMessageRepositoryMock
            .Setup(x => x.GetMaxSequenceNumAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(27);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        conversation.Participants.Should().ContainSingle(p =>
            p.UserId.Value == callerId &&
            p.LastReadMessageSequenceNum == 27);
        conversation.Participants.Should().ContainSingle(p =>
            p.UserId.Value == otherParticipantId &&
            p.LastReadMessageSequenceNum == 0);
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(command, conversation, MutationType.Updated, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenParticipantIsMissing_ReturnsUnauthorized()
    {
        var conversationId = Guid.NewGuid();
        var conversation = CreateGroupConversation(conversationId, Guid.NewGuid(), Guid.NewGuid());
        var command = new MarkConversationAsReadCommand(conversationId, Guid.NewGuid());

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unauthorized);
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<MarkConversationAsReadCommand>(),
                It.IsAny<ConversationAggregate>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static GroupConversation CreateGroupConversation(
        Guid conversationId,
        Guid firstParticipantId,
        Guid secondParticipantId)
    {
        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(conversationId),
            firstParticipantId,
            [firstParticipantId, secondParticipantId],
            "Dev Team");

        conversation.ClearEvents();
        return conversation;
    }
}
