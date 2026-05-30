using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using GroupConversation = FlowChat.ChatService.Domain.Entities.Conversation.GroupConversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.AddParticipant;

public sealed class AddParticipantCommandHandler
    : IdempotentCommandHandlerBase<AddParticipantCommand, bool>
{
    private readonly IGroupConversationWriteRepository _groupConversationRepository;
    private GroupConversation? _conversation;

    public AddParticipantCommandHandler(
        IGroupConversationWriteRepository groupConversationRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IDbUpdateExceptionClassifier dbUpdateExceptionClassifier)
        : base(domainEventDispatcher, unitOfWork, dbUpdateExceptionClassifier)
    {
        _groupConversationRepository = groupConversationRepository ?? throw new ArgumentNullException(nameof(groupConversationRepository));
    }

    protected override async Task<FlowChatResult<bool>> ExecuteCommandAsync(
        AddParticipantCommand request,
        CancellationToken cancellationToken)
    {
        _conversation = await _groupConversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (_conversation is null)
            return FlowChatResult<bool>.Failure(DomainError.NotFound("Conversation not found."));

        var anyAdded = false;
        foreach (var participantUserId in request.ParticipantUserIds)
        {
            if (_conversation.Participants.Any(p => p.UserId == participantUserId))
                continue;

            _conversation.AddParticipant(participantUserId, displayName: null, avatarUrl: null);
            anyAdded = true;
        }

        if (!anyAdded)
            return FlowChatResult<bool>.Success(false);

        await _groupConversationRepository.UpdateAsync(_conversation, cancellationToken);

        return FlowChatResult<bool>.Success(true);
    }

    protected override Task<(bool Found, bool Value)> TryGetExistingResponseAsync(
        AddParticipantCommand request,
        CancellationToken cancellationToken)
        => Task.FromResult((true, false));

    protected override IAggregateRoot? GetAggregateRoot() =>
        _conversation;

    protected override string GetIdempotencyConflictKey(AddParticipantCommand request) =>
        AddParticipantCommand.IdempotencyConflictKey;
}
