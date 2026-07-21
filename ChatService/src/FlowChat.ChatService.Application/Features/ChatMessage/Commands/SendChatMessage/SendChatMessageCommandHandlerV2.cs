using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;

public sealed class SendChatMessageCommandHandlerV2(
    IChatMessageV2WriteRepository messageRepository,
    IConversationParticipantWriteRepository participantRepository,
    IConversationV2WriteRepository conversationRepository,
    IUnitOfWork unitOfWork,
    ILocalEventDispatcher dispatcher,
    IEnumerable<IAggregateBeforeSaveProcessorV2<SendChatMessageCommandV2, ChatMessageV2>> processors)
    : AggregateRootInsertCommandHandlerBaseV3<
        SendChatMessageCommandV2,
        SendChatMessageCommandResultV2,
        ChatMessageV2>(dispatcher, unitOfWork, processors)
{
    private ChatMessageV2? _message;

    protected override async Task<FlowChatResult<AggregateMutation<SendChatMessageCommandResultV2>>> ExecuteAsync(
        SendChatMessageCommandV2 request,
        CancellationToken cancellationToken)
    {
        var conversationId = Id<ConversationV2>.FromGuid(request.ConversationId);
        if (await conversationRepository.GetByIdAsync(conversationId, cancellationToken) is null)
        {
            return Failure(
                DomainError.NotFound("Conversation not found."));
        }

        var participants = await participantRepository.GetActiveByConversationIdAsync(
            conversationId,
            cancellationToken);
        var senderUserId = Id<UserProfileMarker>.FromGuid(request.SenderUserId);

        if (participants.All(x => x.UserId != senderUserId))
        {
            return Failure(
                DomainError.Unauthorized("Sender is not a participant of this conversation."));
        }

        if (participants.Any(x => x.UserId != senderUserId && x.IsBlocked))
        {
            return Failure(
                DomainError.Unauthorized("Recipient has blocked this conversation."));
        }

        _message = ChatMessageV2.Create(
            Id<ChatMessageV2>.FromGuid(request.Id),
            conversationId,
            senderUserId,
            request.Text!);
        await messageRepository.AddAsync(_message, cancellationToken);

        return Created(
            new SendChatMessageCommandResultV2(_message.Id.Value, _message.SentAtUtc.Value));
    }

    protected override ChatMessageV2 GetAggregateRoot() =>
        _message ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
