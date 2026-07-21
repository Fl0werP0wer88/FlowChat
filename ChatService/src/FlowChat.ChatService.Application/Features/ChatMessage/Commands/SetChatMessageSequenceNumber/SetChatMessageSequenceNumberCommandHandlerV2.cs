using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.SetChatMessageSequenceNumber;

public sealed class SetChatMessageSequenceNumberCommandHandlerV2(
    IChatMessageV2WriteRepository messageRepository,
    IConversationMessageSequenceRepositoryV2 sequenceRepository,
    IUnitOfWork unitOfWork,
    ILocalEventDispatcher dispatcher,
    IEnumerable<IAggregateBeforeSaveProcessorV2<SetChatMessageSequenceNumberCommandV2, ChatMessageV2>> processors)
    : AggregateRootUpdateCommandHandlerBaseV3<
        SetChatMessageSequenceNumberCommandV2,
        long,
        ChatMessageV2>(dispatcher, unitOfWork, processors)
{
    protected override async Task<FlowChatResult<ChatMessageV2?>> FetchAggregateRootAsync(
        SetChatMessageSequenceNumberCommandV2 request,
        CancellationToken cancellationToken)
    {
        var message = await messageRepository.GetByIdAsync(
            Id<ChatMessageV2>.FromGuid(request.MessageId),
            cancellationToken);
        return message is null
            ? FlowChatResult<ChatMessageV2?>.Failure(DomainError.NotFound("Chat message not found."))
            : FlowChatResult<ChatMessageV2?>.Success(message);
    }

    protected override async Task<FlowChatResult<AggregateMutation<long>>> ExecuteAsync(
        SetChatMessageSequenceNumberCommandV2 request,
        CancellationToken cancellationToken)
    {
        if (AggregateRoot!.ConversationId.Value != request.ConversationId)
        {
            return Failure(DomainError.NotFound("Chat message not found."));
        }

        if (AggregateRoot.SequenceNum.HasValue)
            return Unchanged(AggregateRoot.SequenceNum.Value);

        var sequence = await sequenceRepository.GetNextAsync(
            Id<ConversationV2>.FromGuid(request.ConversationId),
            cancellationToken);
        AggregateRoot.SetSequenceNumber(sequence);
        return Updated(sequence);
    }
}
