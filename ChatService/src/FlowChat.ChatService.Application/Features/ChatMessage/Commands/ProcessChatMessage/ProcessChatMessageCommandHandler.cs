using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.ProcessChatMessage;

public sealed class ProcessChatMessageCommandHandler(
    IChatMessageWriteRepository chatMessageRepository,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher domainEventDispatcher)
    : CommandHandlerBase<ProcessChatMessageCommand, Unit>(domainEventDispatcher, unitOfWork)
{
    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        ProcessChatMessageCommand request,
        CancellationToken cancellationToken)
    {
        var message = await chatMessageRepository.GetByIdAsync(request.MessageId, cancellationToken);
        if (message is null)
            return FlowChatResult<Unit>.Failure(DomainError.NotFound("Chat message not found."));

        if (message.SequenceNum.HasValue)
            return FlowChatResult<Unit>.Success(Unit.Value);

        var maxSequenceNum = await chatMessageRepository.GetMaxSequenceNumAsync(request.ConversationId, cancellationToken);
        message.MarkAsProcessed(maxSequenceNum.GetValueOrDefault() + 1);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Unit> result) => null;
}
