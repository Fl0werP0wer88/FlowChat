using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using MediatR;
using GroupConversation = FlowChat.ChatService.Domain.Entities.Conversation.GroupConversation;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.AddGroupParticipants;

public sealed class AddGroupParticipantsCommandHandler
    : AggregateRootUpdateCommandHandlerBaseV3<AddGroupParticipantsCommand, Unit, GroupConversation>
{
    private readonly IGroupConversationWriteRepository _groupConversationRepository;
    private readonly IChatMessageWriteRepository _chatMessageRepository;
    private GroupConversation? _conversation;

    public AddGroupParticipantsCommandHandler(
        IGroupConversationWriteRepository groupConversationRepository,
        IChatMessageWriteRepository chatMessageRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<AddGroupParticipantsCommand, GroupConversation>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _groupConversationRepository = groupConversationRepository ?? throw new ArgumentNullException(nameof(groupConversationRepository));
        _chatMessageRepository = chatMessageRepository ?? throw new ArgumentNullException(nameof(chatMessageRepository));
    }

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        AddGroupParticipantsCommand request,
        CancellationToken cancellationToken)
    {
        _conversation = await _groupConversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (_conversation is null)
            return FlowChatResult<Unit>.Failure(DomainError.NotFound("Conversation not found."));

        var newParticipantUserIds = request.ParticipantUserIds
            .Select(Id<UserProfileMarker>.FromGuid)
            .Distinct()
            .Where(participantUserId => _conversation.Participants.All(p => p.UserId != participantUserId))
            .ToList();

        if (newParticipantUserIds.Count == 0)
        {
            return FlowChatResult<Unit>.Success(Unit.Value);
        }

        var maxSequenceNum = await _chatMessageRepository.GetMaxSequenceNumAsync(
            request.ConversationId,
            cancellationToken);
        var lastReadMessageSequenceNum = maxSequenceNum.GetValueOrDefault();

        _conversation.AddParticipants(
            newParticipantUserIds,
            displayName: null,
            avatarUrl: null,
            lastReadMessageSequenceNum);

        SetUpdated();

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override GroupConversation GetAggregateRoot() =>
        _conversation ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
