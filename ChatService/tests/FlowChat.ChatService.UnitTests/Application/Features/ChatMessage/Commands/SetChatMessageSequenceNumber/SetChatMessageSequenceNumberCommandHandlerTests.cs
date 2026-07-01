using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Commands.SetChatMessageSequenceNumber;
using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;
using ChatMessageAggregate = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessage;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.UnitTests.Application.Features.ChatMessage.Commands.SetChatMessageSequenceNumber;

public sealed class SetChatMessageSequenceNumberCommandHandlerTests
{
    private readonly Mock<IChatMessageWriteRepository> _chatMessageRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILocalEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IAggregateBeforeSaveProcessor<SetChatMessageSequenceNumberCommand, ChatMessageAggregate>> _beforeSaveProcessorMock = new();
    private readonly SetChatMessageSequenceNumberCommandHandler _handler;

    public SetChatMessageSequenceNumberCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<long>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<long>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _beforeSaveProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<SetChatMessageSequenceNumberCommand>(),
                It.IsAny<ChatMessageAggregate>(),
                It.IsAny<AggregateState>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new SetChatMessageSequenceNumberCommandHandler(
            _chatMessageRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            [_beforeSaveProcessorMock.Object]);
    }

    [Fact]
    public async Task Handle_WhenMessageHasNoSequenceNumber_AssignsNextSequenceNumber()
    {
        var conversationId = Guid.NewGuid();
        var message = CreateMessage(conversationId);
        var command = new SetChatMessageSequenceNumberCommand(message.Id.Value, conversationId);

        _chatMessageRepositoryMock
            .Setup(x => x.GetByIdAsync(command.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);
        _chatMessageRepositoryMock
            .Setup(x => x.GetMaxSequenceNumAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(41);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        message.SequenceNum.Should().Be(42);
        message.DeliveryStatus.Should().Be(DeliveryStatus.Pending);
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(command, message, AggregateState.Updated, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMessageAlreadyHasSequenceNumber_ReturnsExistingSequenceNumberWithoutChanges()
    {
        var conversationId = Guid.NewGuid();
        var message = CreateMessage(conversationId);
        message.SetSequenceNumber(12);
        message.ClearEvents();
        var initialVersion = message.Version;
        var command = new SetChatMessageSequenceNumberCommand(message.Id.Value, conversationId);

        _chatMessageRepositoryMock
            .Setup(x => x.GetByIdAsync(command.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(12);
        message.Version.Should().Be(initialVersion);
        _chatMessageRepositoryMock.Verify(
            x => x.GetMaxSequenceNumAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<SetChatMessageSequenceNumberCommand>(),
                It.IsAny<ChatMessageAggregate>(),
                It.IsAny<AggregateState>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMessageIsMissing_ReturnsNotFound()
    {
        var command = new SetChatMessageSequenceNumberCommand(Guid.NewGuid(), Guid.NewGuid());

        _chatMessageRepositoryMock
            .Setup(x => x.GetByIdAsync(command.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatMessageAggregate?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    private static ChatMessageAggregate CreateMessage(Guid conversationId)
    {
        var message = ChatMessageAggregate.Create(
            Id<ChatMessageAggregate>.New(),
            Id<ConversationAggregate>.FromGuid(conversationId),
            Guid.NewGuid(),
            "Alice",
            "Hello",
            [Guid.NewGuid()]);

        message.ClearEvents();
        return message;
    }
}
