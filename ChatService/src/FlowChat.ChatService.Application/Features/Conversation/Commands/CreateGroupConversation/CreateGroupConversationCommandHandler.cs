using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;
using GroupConversation = FlowChat.ChatService.Domain.Entities.Conversation.GroupConversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;

public sealed class CreateGroupConversationCommandHandler
    : IdempotentCommandHandlerBase<CreateGroupConversationCommand, Guid>
{
    private readonly IGroupConversationWriteRepository _conversationRepository;
    private GroupConversation? _conversation;

    public CreateGroupConversationCommandHandler(
        IGroupConversationWriteRepository conversationRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher,
        IDbUpdateExceptionClassifier dbUpdateExceptionClassifier)
        : base(domainEventDispatcher, unitOfWork, dbUpdateExceptionClassifier)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteCommandAsync(
        CreateGroupConversationCommand request,
        CancellationToken cancellationToken)
    {
        _conversation = GroupConversation.Create(
            Id<ConversationAggregate>.FromGuid(request.ConversationId),
            request.CreatedByUserId,
            request.ParticipantUserIds,
            request.Name);

        await _conversationRepository.AddAsync(_conversation, cancellationToken);

        return FlowChatResult<Guid>.Success(_conversation.Id.Value);
    }

    protected override async Task<(bool Found, Guid Value)> TryGetExistingResponseAsync(
        CreateGroupConversationCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        return existing is not null
            ? (true, existing.Id.Value)
            : (false, default);
    }

    protected override IAggregateRoot? GetExecutedAggregateRoot(IdempotentCommandResult<Guid> result) =>
        _conversation;

    protected override string GetIdempotencyConflictKey(CreateGroupConversationCommand request) =>
        CreateGroupConversationCommand.IdempotencyConflictKey;
}
