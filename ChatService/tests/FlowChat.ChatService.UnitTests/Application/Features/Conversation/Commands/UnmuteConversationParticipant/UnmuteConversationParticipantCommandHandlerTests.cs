using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Commands.UnmuteConversationParticipant;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;
using DuetConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Commands.UnmuteConversationParticipant;

public sealed class UnmuteConversationParticipantCommandHandlerTests
{
    private readonly Mock<IDuetConversationWriteRepository> _duetConversationRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILocalEventDispatcher> _domainEventDispatcherMock = new();
    private readonly UnmuteConversationParticipantCommandHandler _handler;

    public UnmuteConversationParticipantCommandHandlerTests()
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

        _handler = new UnmuteConversationParticipantCommandHandler(
            _duetConversationRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            []);
    }

    [Fact]
    public async Task Handle_WhenConversationIsMissing_ReturnsNotFound()
    {
        var command = new UnmuteConversationParticipantCommand(Guid.NewGuid(), Guid.NewGuid());

        _duetConversationRepositoryMock
            .Setup(x => x.GetByIdAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DuetConversationAggregate?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenMuted_SetsMutedFalse()
    {
        var requestingUserId = Guid.NewGuid();
        var conversation = DuetConversationAggregate.Create(requestingUserId, Guid.NewGuid());
        conversation.MuteParticipant(requestingUserId);
        conversation.ClearEvents();
        var command = new UnmuteConversationParticipantCommand(conversation.Id.Value, requestingUserId);

        _duetConversationRepositoryMock
            .Setup(x => x.GetByIdAsync(command.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        conversation.GetParticipant(requestingUserId)!.IsMuted.Should().BeFalse();
    }
}
