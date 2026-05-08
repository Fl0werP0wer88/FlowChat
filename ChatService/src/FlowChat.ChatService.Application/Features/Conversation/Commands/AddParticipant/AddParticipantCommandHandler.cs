using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using GroupConversation = FlowChat.ChatService.Domain.Entities.Conversation.GroupConversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.AddParticipant;

public sealed class AddParticipantCommandHandler
    : IdempotentCommandHandlerBase<AddParticipantCommand, bool>
{
    private readonly IGroupConversationWriteRepository _conversationRepository;
    private readonly IConversationParticipantReadRepository _participantReadRepository;
    private GroupConversation? _conversation;

    public AddParticipantCommandHandler(
        IGroupConversationWriteRepository conversationRepository,
        IConversationParticipantReadRepository participantReadRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher,
        IDbUpdateExceptionClassifier dbUpdateExceptionClassifier)
        : base(domainEventDispatcher, unitOfWork, dbUpdateExceptionClassifier)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _participantReadRepository = participantReadRepository ?? throw new ArgumentNullException(nameof(participantReadRepository));
    }

    protected override async Task<FlowChatResult<bool>> ExecuteCommandAsync(
        AddParticipantCommand request,
        CancellationToken cancellationToken)
    {
        _conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (_conversation is null)
        {
            var participants = await _participantReadRepository.GetParticipantUserIdsAsync(request.ConversationId, cancellationToken);
            return participants is null
                ? FlowChatResult<bool>.Failure(DomainError.NotFound("Conversation not found."))
                : FlowChatResult<bool>.Failure(DomainError.BadRequest("Cannot add participants to a one-on-one conversation."));
        }

        if (_conversation.Participants.Any(p => p.UserId == request.ParticipantUserId))
            return FlowChatResult<bool>.Success(false);

        _conversation.AddParticipant(request.ParticipantUserId, displayName: null, avatarUrl: null);

        await _conversationRepository.UpdateAsync(_conversation, cancellationToken);

        return FlowChatResult<bool>.Success(true);
    }

    protected override Task<(bool Found, bool Value)> TryGetExistingResponseAsync(
        AddParticipantCommand request,
        CancellationToken cancellationToken)
        => Task.FromResult((true, false));

    // Dispatch domain events only when participant was newly added (Value = true).
    protected override IAggregateRoot? GetExecutedAggregateRoot(IdempotentCommandResult<bool> result) =>
        result.Value ? _conversation : null;

    protected override string GetIdempotencyConflictKey(AddParticipantCommand request) =>
        AddParticipantCommand.IdempotencyConflictKey;
}
