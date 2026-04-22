using FlowChat.Shared.Application;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Shared.Domain;
using ChatMessageAggregate = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessage;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;

public sealed class SendChatMessageCommandHandler
    : CommandHandlerBase<SendChatMessageCommand, Guid>
{
    private readonly IChatMessageWriteRepository _chatMessageRepository;
    private readonly IConversationWriteRepository _conversationRepository;
    private ChatMessageAggregate? _chatMessage;

    public SendChatMessageCommandHandler(
        IChatMessageWriteRepository chatMessageRepository,
        IConversationWriteRepository conversationRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
        : base(domainEventDispatcher, unitOfWork)
    {
        _chatMessageRepository = chatMessageRepository ?? throw new ArgumentNullException(nameof(chatMessageRepository));
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
        SendChatMessageCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);

        if (conversation is null)
            return FlowChatResult<Guid>.Failure(DomainError.NotFound("Conversation not found."));

        if (!conversation.Participants.Any(p => p.UserId == request.SenderUserId))
            return FlowChatResult<Guid>.Failure(DomainError.Unauthorized("Sender is not a participant of this conversation."));

        var recipientUserIds = conversation.Participants
            .Select(p => p.UserId)
            .Where(id => id != request.SenderUserId && id != Guid.Empty)
            .Distinct()
            .ToArray();

        _chatMessage = ChatMessageAggregate.Create(
            request.ConversationId,
            request.SenderUserId,
            request.SenderDisplayName!.Trim(),
            request.Text!.Trim(),
            recipientUserIds);

        await _chatMessageRepository.AddAsync(_chatMessage, cancellationToken);

        return FlowChatResult<Guid>.Success(_chatMessage.Id.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Guid> result) =>
        result.IsSuccess ? _chatMessage : null;
}
