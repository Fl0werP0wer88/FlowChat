using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Commands.MarkChatMessageAsDelivered;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Moq;
using ChatMessageAggregate = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessage;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.UnitTests.Application.Features.ChatMessage.Commands.MarkChatMessageAsDelivered;

public sealed class MarkChatMessageAsDeliveredCommandHandlerTests
{
    private readonly Mock<IChatMessageWriteRepository> _chatMessageRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILocalEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IAggregateBeforeSaveProcessor<MarkChatMessageAsDeliveredCommand, ChatMessageAggregate>> _beforeSaveProcessorMock = new();
    private readonly MarkChatMessageAsDeliveredCommandHandler _handler;

    public MarkChatMessageAsDeliveredCommandHandlerTests()
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
                It.IsAny<MarkChatMessageAsDeliveredCommand>(),
                It.IsAny<ChatMessageAggregate>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new MarkChatMessageAsDeliveredCommandHandler(
            _chatMessageRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            [_beforeSaveProcessorMock.Object]);
    }

    [Fact]
    public async Task Handle_WhenMessageIsPending_MarksDeliveredAndProcessesUpdatedMutationType()
    {
        var conversationId = Guid.NewGuid();
        var message = CreateMessage(conversationId);
        message.SetSequenceNumber(42);
        var command = new MarkChatMessageAsDeliveredCommand(
            message.Id.Value,
            conversationId,
            DateTimeOffset.UtcNow);

        _chatMessageRepositoryMock
            .Setup(x => x.GetByIdAsync(command.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        message.SequenceNum.Should().Be(42);
        message.DeliveredAtUtc.Should().NotBeNull();
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(command, message, MutationType.Updated, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMessageAlreadyDelivered_ReturnsSuccessWithoutProcessingAggregateChanges()
    {
        var conversationId = Guid.NewGuid();
        var message = CreateMessage(conversationId);
        message.SetSequenceNumber(12);
        message.MarkAsDelivered(UtcDateTimeOffset.UtcNow);
        message.ClearEvents();
        var initialVersion = message.Version;
        var command = new MarkChatMessageAsDeliveredCommand(
            message.Id.Value,
            conversationId,
            DateTimeOffset.UtcNow);

        _chatMessageRepositoryMock
            .Setup(x => x.GetByIdAsync(command.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        message.Version.Should().Be(initialVersion);
        _domainEventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<MarkChatMessageAsDeliveredCommand>(),
                It.IsAny<ChatMessageAggregate>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMessageIsMissing_ReturnsNotFound()
    {
        var command = new MarkChatMessageAsDeliveredCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        _chatMessageRepositoryMock
            .Setup(x => x.GetByIdAsync(command.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatMessageAggregate?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenMessageHasNoSequenceNumber_ReturnsBadRequest()
    {
        var conversationId = Guid.NewGuid();
        var message = CreateMessage(conversationId);
        var command = new MarkChatMessageAsDeliveredCommand(
            message.Id.Value,
            conversationId,
            DateTimeOffset.UtcNow);

        _chatMessageRepositoryMock
            .Setup(x => x.GetByIdAsync(command.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<MarkChatMessageAsDeliveredCommand>(),
                It.IsAny<ChatMessageAggregate>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static ChatMessageAggregate CreateMessage(Guid conversationId)
    {
        var message = ChatMessageAggregate.Create(
            Id<ChatMessageAggregate>.New(),
            Id<ConversationAggregate>.FromGuid(conversationId),
            Guid.NewGuid(),
            "Hello",
            [Guid.NewGuid()]);

        message.ClearEvents();
        return message;
    }
}
