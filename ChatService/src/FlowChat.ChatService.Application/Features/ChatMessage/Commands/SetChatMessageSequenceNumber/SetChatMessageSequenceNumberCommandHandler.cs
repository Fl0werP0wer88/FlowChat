using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using ChatMessageAggregate = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessage;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.SetChatMessageSequenceNumber;

public sealed class SetChatMessageSequenceNumberCommandHandler(
    IChatMessageWriteRepository chatMessageRepository,
    IConversationMessageSequenceRepository sequenceRepository,
    IUnitOfWork unitOfWork,
    ILocalEventDispatcher domainEventDispatcher,
    IEnumerable<IAggregateBeforeSaveProcessorV2<SetChatMessageSequenceNumberCommand, ChatMessageAggregate>> beforeSaveProcessors)
    : AggregateRootUpdateCommandHandlerBaseV3<SetChatMessageSequenceNumberCommand, long, ChatMessageAggregate>(
        domainEventDispatcher,
        unitOfWork,
        beforeSaveProcessors)
{
    protected override async Task<FlowChatResult<ChatMessageAggregate?>> FetchAggregateRootAsync(
        SetChatMessageSequenceNumberCommand request,
        CancellationToken cancellationToken)
    {
        var message = await chatMessageRepository.GetByIdAsync(request.MessageId, cancellationToken);
        if (message is null)
            return FlowChatResult<ChatMessageAggregate?>.Failure(DomainError.NotFound("Chat message not found."));

        return FlowChatResult<ChatMessageAggregate?>.Success(message);
    }

    protected override async Task<FlowChatResult<AggregateMutation<long>>> ExecuteAsync(
        SetChatMessageSequenceNumberCommand request,
        CancellationToken cancellationToken)
    {
        if (AggregateRoot!.SequenceNum.HasValue)
        {
            return Unchanged(AggregateRoot.SequenceNum.Value);
        }

        var sequenceNum = await sequenceRepository.GetNextAsync(request.ConversationId, cancellationToken);
        AggregateRoot.SetSequenceNumber(sequenceNum);

        return Updated(sequenceNum);
    }
}
