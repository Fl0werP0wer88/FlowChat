using FlowChat.Shared.Application;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.ChatMessages.Commands.SendChatMessage;

public sealed class SendChatMessageCommandHandler
    : CommandHandlerBase<SendChatMessageCommand, Guid>
{
    private readonly IChatMessageWriteRepository _chatMessageRepository;
    private ChatMessage? _chatMessage;

    public SendChatMessageCommandHandler(
        IChatMessageWriteRepository chatMessageRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
        : base(domainEventDispatcher, unitOfWork)
    {
        _chatMessageRepository = chatMessageRepository ?? throw new ArgumentNullException(nameof(chatMessageRepository));
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
        SendChatMessageCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedRecipientUserIds = (request.RecipientUserIds ?? Array.Empty<Guid>())
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();

        _chatMessage = ChatMessage.Create(
            request.ConversationId,
            request.SenderUserId,
            request.SenderDisplayName!.Trim(),
            request.Text!.Trim(),
            normalizedRecipientUserIds);

        await _chatMessageRepository.AddAsync(_chatMessage, cancellationToken);

        return FlowChatResult<Guid>.Success(_chatMessage.Id.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Guid> result) =>
        result.IsSuccess ? _chatMessage : null;
}

