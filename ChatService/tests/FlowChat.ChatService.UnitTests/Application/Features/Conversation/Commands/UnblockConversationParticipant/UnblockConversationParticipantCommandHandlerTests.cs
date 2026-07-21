using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Commands.UnblockConversationParticipant;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;
using DuetConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Commands.UnblockConversationParticipant;

public sealed class UnblockConversationParticipantCommandHandlerTests
{
    private readonly Mock<IDuetConversationWriteRepository> _duetConversationRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILocalEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IAggregateBeforeSaveProcessorV2<UnblockConversationParticipantCommand, DuetConversationAggregate>> _beforeSaveProcessorMock = new();
    private readonly UnblockConversationParticipantCommandHandler _handler;

    public UnblockConversationParticipantCommandHandlerTests()
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
                It.IsAny<UnblockConversationParticipantCommand>(),
                It.IsAny<DuetConversationAggregate>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new UnblockConversationParticipantCommandHandler(
            _duetConversationRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            [_beforeSaveProcessorMock.Object]);
    }

    [Fact]
    public async Task Handle_WhenConversationIsMissing_ReturnsNotFound()
    {
        var command = new UnblockConversationParticipantCommand(Guid.NewGuid(), Guid.NewGuid());

        _duetConversationRepositoryMock
            .Setup(x => x.GetByIdAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DuetConversationAggregate?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenNotBlocked_ReturnsConflict()
    {
        var requestingUserId = Guid.NewGuid();
        var conversation = DuetConversationAggregate.Create(requestingUserId, Guid.NewGuid());
        var command = new UnblockConversationParticipantCommand(conversation.Id.Value, requestingUserId);

        _duetConversationRepositoryMock
            .Setup(x => x.GetByIdAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_WhenBlocked_UnblocksAndInvokesProcessor()
    {
        var requestingUserId = Guid.NewGuid();
        var conversation = DuetConversationAggregate.Create(requestingUserId, Guid.NewGuid());
        conversation.BlockParticipant(requestingUserId);
        conversation.ClearEvents();
        var command = new UnblockConversationParticipantCommand(conversation.Id.Value, requestingUserId);

        _duetConversationRepositoryMock
            .Setup(x => x.GetByIdAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        conversation.GetParticipant(requestingUserId)!.IsBlocked.Should().BeFalse();
        _beforeSaveProcessorMock.Verify(
            x => x.ProcessAsync(command, conversation, MutationType.Updated, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
