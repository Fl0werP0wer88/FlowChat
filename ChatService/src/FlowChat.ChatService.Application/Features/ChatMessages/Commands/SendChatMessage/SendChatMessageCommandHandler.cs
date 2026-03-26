using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities;
using FlowChat.Domain.Abstractions;

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
        var validationErrors = new ValidationErrorCollector()
            .AddIf(request.ConversationId == Guid.Empty, "ConversationId is required.")
            .AddIf(request.SenderUserId == Guid.Empty, "SenderUserId is required.")
            .AddIf(string.IsNullOrWhiteSpace(request.SenderDisplayName), "SenderDisplayName is required.")
            .AddIf(string.IsNullOrWhiteSpace(request.Text), "Text is required.");

        var normalizedRecipientUserIds = (request.RecipientUserIds ?? Array.Empty<Guid>())
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();

        validationErrors.AddIf(
            normalizedRecipientUserIds.Length == 0,
            "RecipientUserIds must contain at least one valid user id.");

        if (validationErrors.HasErrors)
        {
            return validationErrors.ToFailure<Guid>();
        }

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
