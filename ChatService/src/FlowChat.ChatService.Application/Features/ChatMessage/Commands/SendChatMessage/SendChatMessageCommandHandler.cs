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
    : AggregateRootInsertCommandHandlerBaseV2<SendChatMessageCommand, SendChatMessageCommandResult, ChatMessageAggregate>
{
    private readonly IChatMessageWriteRepository _chatMessageRepository;
    private readonly IConversationParticipantReadRepository _participantReadRepository;
    private ChatMessageAggregate? _chatMessage;

    public SendChatMessageCommandHandler(
        IChatMessageWriteRepository chatMessageRepository,
        IConversationParticipantReadRepository participantReadRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<SendChatMessageCommand, ChatMessageAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _chatMessageRepository = chatMessageRepository ?? throw new ArgumentNullException(nameof(chatMessageRepository));
        _participantReadRepository = participantReadRepository ?? throw new ArgumentNullException(nameof(participantReadRepository));
    }

    protected override async Task<FlowChatResult<SendChatMessageCommandResult>> ExecuteAsync(
        SendChatMessageCommand request,
        CancellationToken cancellationToken)
    {
        var participantUserIds = await _participantReadRepository.GetParticipantUserIdsAsync(
            request.ConversationId,
            cancellationToken);

        if (participantUserIds is null)
            return FlowChatResult<SendChatMessageCommandResult>.Failure(DomainError.NotFound("Conversation not found."));

        if (!participantUserIds.Contains(request.SenderUserId))
            return FlowChatResult<SendChatMessageCommandResult>.Failure(DomainError.Unauthorized("Sender is not a participant of this conversation."));

        var recipientUserIds = participantUserIds
            .Where(id => id != request.SenderUserId && id != Guid.Empty)
            .Distinct()
            .ToArray();

        _chatMessage = ChatMessageAggregate.Create(
            Id<ChatMessageAggregate>.FromGuid(request.Id),
            Id<FlowChat.ChatService.Domain.Entities.Conversation.Conversation>.FromGuid(request.ConversationId),
            Id<UserProfileMarker>.FromGuid(request.SenderUserId),
            request.SenderDisplayName!.Trim(),
            request.Text!.Trim(),
            recipientUserIds.Select(Id<UserProfileMarker>.FromGuid));

        await _chatMessageRepository.AddAsync(_chatMessage, cancellationToken);

        return FlowChatResult<SendChatMessageCommandResult>.Success(
            new SendChatMessageCommandResult(_chatMessage.Id.Value, _chatMessage.SentAtUtc.Value));
    }

    protected override ChatMessageAggregate GetAggregateRoot() =>
        _chatMessage ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
