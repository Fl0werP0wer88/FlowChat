using FlowChat.Shared.Application;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using ChatMessageAggregate = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessage;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;

public sealed class SendChatMessageCommandHandler
    : IdempotentCommandHandlerBase<SendChatMessageCommand, Guid>
{
    private readonly IChatMessageWriteRepository _chatMessageRepository;
    private readonly IConversationParticipantReadRepository _participantReadRepository;
    private ChatMessageAggregate? _chatMessage;

    public SendChatMessageCommandHandler(
        IChatMessageWriteRepository chatMessageRepository,
        IConversationParticipantReadRepository participantReadRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher,
        IDbUpdateExceptionClassifier dbUpdateExceptionClassifier)
        : base(domainEventDispatcher, unitOfWork, dbUpdateExceptionClassifier)
    {
        _chatMessageRepository = chatMessageRepository ?? throw new ArgumentNullException(nameof(chatMessageRepository));
        _participantReadRepository = participantReadRepository ?? throw new ArgumentNullException(nameof(participantReadRepository));
    }

    protected override async Task<(bool Found, Guid Value)> TryGetExistingResponseAsync(
        SendChatMessageCommand request,
        CancellationToken cancellationToken)
    {
        var message = await _chatMessageRepository.GetByIdAsync(request.Id, cancellationToken);
        if (message is null
            || message.ConversationId.Value != request.ConversationId
            || message.SenderUserId != request.SenderUserId)
        {
            return (false, default);
        }

        return (true, message.Id.Value);
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteCommandAsync(
        SendChatMessageCommand request,
        CancellationToken cancellationToken)
    {
        var participantUserIds = await _participantReadRepository.GetParticipantUserIdsAsync(
            request.ConversationId,
            cancellationToken);

        if (participantUserIds is null)
            return FlowChatResult<Guid>.Failure(DomainError.NotFound("Conversation not found."));

        if (!participantUserIds.Contains(request.SenderUserId))
            return FlowChatResult<Guid>.Failure(DomainError.Unauthorized("Sender is not a participant of this conversation."));

        var recipientUserIds = participantUserIds
            .Where(id => id != request.SenderUserId && id != Guid.Empty)
            .Distinct()
            .ToArray();

        _chatMessage = ChatMessageAggregate.Create(
            Id<ChatMessageAggregate>.FromGuid(request.Id),
            Id<FlowChat.ChatService.Domain.Entities.Conversation.Conversation>.FromGuid(request.ConversationId),
            request.SenderUserId,
            request.SenderDisplayName!.Trim(),
            request.Text!.Trim(),
            recipientUserIds);

        await _chatMessageRepository.AddAsync(_chatMessage, cancellationToken);

        return FlowChatResult<Guid>.Success(_chatMessage.Id.Value);
    }

    protected override IAggregateRoot? GetExecutedAggregateRoot(IdempotentCommandResult<Guid> result) =>
        _chatMessage;

    protected override string GetIdempotencyConflictKey(SendChatMessageCommand request) =>
        SendChatMessageCommand.IdempotencyConflictKey;
}
