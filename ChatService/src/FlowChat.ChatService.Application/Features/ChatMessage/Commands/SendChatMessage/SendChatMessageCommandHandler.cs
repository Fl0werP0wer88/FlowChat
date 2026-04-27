using FlowChat.Shared.Application;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Shared.Domain;
using ChatMessageAggregate = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessage;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;

public sealed class SendChatMessageCommandHandler
    : CommandHandlerBase<SendChatMessageCommand, Guid>
{
    private readonly IChatMessageWriteRepository _chatMessageRepository;
    private readonly IConversationParticipantReadRepository _participantReadRepository;
    private ChatMessageAggregate? _chatMessage;

    public SendChatMessageCommandHandler(
        IChatMessageWriteRepository chatMessageRepository,
        IConversationParticipantReadRepository participantReadRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
        : base(domainEventDispatcher, unitOfWork)
    {
        _chatMessageRepository = chatMessageRepository ?? throw new ArgumentNullException(nameof(chatMessageRepository));
        _participantReadRepository = participantReadRepository ?? throw new ArgumentNullException(nameof(participantReadRepository));
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
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

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Guid> result) =>
        result.IsSuccess ? _chatMessage : null;
}
