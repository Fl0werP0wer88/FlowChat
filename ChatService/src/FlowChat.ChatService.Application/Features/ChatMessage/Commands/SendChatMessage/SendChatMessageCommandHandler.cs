using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using ChatMessageAggregate = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessage;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;

public sealed class SendChatMessageCommandHandler
    : AggregateRootInsertCommandHandlerBaseV3<SendChatMessageCommand, SendChatMessageCommandResult, ChatMessageAggregate>
{
    private readonly IChatMessageWriteRepository _chatMessageRepository;
    private readonly IConversationParticipantReadRepository _participantReadRepository;
    private readonly IConversationWriteRepository _conversationWriteRepository;
    private ChatMessageAggregate? _chatMessage;

    public SendChatMessageCommandHandler(
        IChatMessageWriteRepository chatMessageRepository,
        IConversationParticipantReadRepository participantReadRepository,
        IConversationWriteRepository conversationWriteRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<SendChatMessageCommand, ChatMessageAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _chatMessageRepository = chatMessageRepository ?? throw new ArgumentNullException(nameof(chatMessageRepository));
        _participantReadRepository = participantReadRepository ?? throw new ArgumentNullException(nameof(participantReadRepository));
        _conversationWriteRepository = conversationWriteRepository ?? throw new ArgumentNullException(nameof(conversationWriteRepository));
    }

    protected override async Task<FlowChatResult<AggregateMutation<SendChatMessageCommandResult>>> ExecuteAsync(
        SendChatMessageCommand request,
        CancellationToken cancellationToken)
    {
        var participantStates = await _participantReadRepository.GetParticipantStatesAsync(
            request.ConversationId,
            cancellationToken);

        if (participantStates is null)
            return Failure(DomainError.NotFound("Conversation not found."));

        if (participantStates.All(p => p.UserId != request.SenderUserId))
            return Failure(DomainError.Unauthorized("Sender is not a participant of this conversation."));

        var recipients = participantStates
            .Where(p => p.UserId != request.SenderUserId && p.UserId != Guid.Empty)
            .ToArray();

        if (recipients.Any(r => r.IsBlocked))
            return Failure(DomainError.Unauthorized("Recipient has blocked this conversation."));

        var hiddenRecipientIds = recipients.Where(r => r.IsHidden).Select(r => r.UserId).ToArray();
        if (hiddenRecipientIds.Length > 0)
        {
            var conversation = await _conversationWriteRepository.GetByIdAsync(
                Id<FlowChat.ChatService.Domain.Entities.Conversation.Conversation>.FromGuid(request.ConversationId),
                cancellationToken);

            if (conversation is not null)
            {
                foreach (var hiddenRecipientId in hiddenRecipientIds)
                {
                    conversation.UnhideParticipant(Id<UserProfileMarker>.FromGuid(hiddenRecipientId));
                }
            }
        }

        var recipientUserIds = recipients
            .Select(r => r.UserId)
            .Distinct()
            .ToArray();

        _chatMessage = ChatMessageAggregate.Create(
            Id<ChatMessageAggregate>.FromGuid(request.Id),
            Id<FlowChat.ChatService.Domain.Entities.Conversation.Conversation>.FromGuid(request.ConversationId),
            Id<UserProfileMarker>.FromGuid(request.SenderUserId),
            request.Text!.Trim(),
            recipientUserIds.Select(Id<UserProfileMarker>.FromGuid));

        await _chatMessageRepository.AddAsync(_chatMessage, cancellationToken);

        return Created(
            new SendChatMessageCommandResult(_chatMessage.Id.Value, _chatMessage.SentAtUtc.Value));
    }

    protected override ChatMessageAggregate GetAggregateRoot() =>
        _chatMessage ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
