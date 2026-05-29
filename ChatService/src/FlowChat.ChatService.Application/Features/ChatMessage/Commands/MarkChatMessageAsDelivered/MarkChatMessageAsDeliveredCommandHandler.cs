using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using MediatR;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.MarkChatMessageAsDelivered;

public sealed class MarkChatMessageAsDeliveredCommandHandler(
    IChatMessageWriteRepository chatMessageRepository,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher domainEventDispatcher)
    : AggregateRootCommandHandlerBase<MarkChatMessageAsDeliveredCommand, Unit>(domainEventDispatcher, unitOfWork)
{
    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        MarkChatMessageAsDeliveredCommand request,
        CancellationToken cancellationToken)
    {
        var message = await chatMessageRepository.GetByIdAsync(request.MessageId, cancellationToken);
        if (message is null)
            return FlowChatResult<Unit>.Failure(DomainError.NotFound("Chat message not found."));

        if (message.SequenceNum.HasValue)
            return FlowChatResult<Unit>.Success(Unit.Value);

        var maxSequenceNum = await chatMessageRepository.GetMaxSequenceNumAsync(request.ConversationId, cancellationToken);
        message.MarkAsDelivered(
            maxSequenceNum.GetValueOrDefault() + 1,
            UtcDateTimeOffset.Create(request.DeliveredAtUtc));

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

}
