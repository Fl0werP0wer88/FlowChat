using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using GroupConversation = FlowChat.ChatService.Domain.Entities.Conversation.GroupConversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.AddParticipant;

public sealed class AddParticipantCommandHandler
    : AggregateRootUpdateCommandHandlerBaseV2<AddParticipantCommand, bool, GroupConversation>
{
    private readonly IGroupConversationWriteRepository _groupConversationRepository;
    private GroupConversation? _conversation;
    private bool _participantsChanged;

    public AddParticipantCommandHandler(
        IGroupConversationWriteRepository groupConversationRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<AddParticipantCommand, GroupConversation>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _groupConversationRepository = groupConversationRepository ?? throw new ArgumentNullException(nameof(groupConversationRepository));
    }

    protected override async Task<FlowChatResult<bool>> ExecuteAsync(
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
        {
            _participantsChanged = false;
            return FlowChatResult<bool>.Success(false);
        }

        _participantsChanged = true;
        await _groupConversationRepository.UpdateAsync(_conversation, cancellationToken);

        return FlowChatResult<bool>.Success(true);
    }

    protected override GroupConversation GetAggregateRoot() =>
        _conversation ?? throw new InvalidOperationException("Aggregate root instance is not available.");

    protected override AggregateState GetAggregateState(AddParticipantCommand request, GroupConversation aggregateRoot) =>
        _participantsChanged ? AggregateState.Updated : AggregateState.Unchanged;
}
