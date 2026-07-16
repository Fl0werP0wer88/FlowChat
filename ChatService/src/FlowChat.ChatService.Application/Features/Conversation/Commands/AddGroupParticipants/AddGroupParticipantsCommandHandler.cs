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
    private readonly IChatMessageReadRepository _chatMessageRepository;

    public AddGroupParticipantsCommandHandler(
        IGroupConversationWriteRepository groupConversationRepository,
        IChatMessageReadRepository chatMessageRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<AddGroupParticipantsCommand, GroupConversation>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _groupConversationRepository = groupConversationRepository ?? throw new ArgumentNullException(nameof(groupConversationRepository));
        _chatMessageRepository = chatMessageRepository ?? throw new ArgumentNullException(nameof(chatMessageRepository));
    }

    protected override async Task<FlowChatResult<GroupConversation?>> FetchAggregateRootAsync(
        AddGroupParticipantsCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = await _groupConversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
            return FlowChatResult<GroupConversation?>.Failure(DomainError.NotFound("Conversation not found."));

        return FlowChatResult<GroupConversation?>.Success(conversation);
    }

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        AddGroupParticipantsCommand request,
        CancellationToken cancellationToken)
    {
        var newParticipantUserIds = request.ParticipantUserIds
            .Select(Id<UserProfileMarker>.FromGuid)
            .Distinct()
            .Where(participantUserId => AggregateRoot!.Participants.All(p => p.UserId != participantUserId))
            .ToList();

        if (newParticipantUserIds.Count == 0)
        {
            return FlowChatResult<Unit>.Success(Unit.Value);
        }

        var maxSequenceNum = await _chatMessageRepository.GetMaxSequenceNumAsync(
            request.ConversationId,
            cancellationToken);
        var lastReadMessageSequenceNum = maxSequenceNum.GetValueOrDefault();

        AggregateRoot!.AddParticipants(
            newParticipantUserIds,
            displayName: null,
            lastReadMessageSequenceNum);

        SetUpdated();

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
