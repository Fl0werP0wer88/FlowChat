using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using MediatR;
using GroupConversation = FlowChat.ChatService.Domain.Entities.Conversation.GroupConversation;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.RemoveGroupParticipants;

public sealed class RemoveGroupParticipantsCommandHandler
    : AggregateRootUpdateCommandHandlerBaseV3<RemoveGroupParticipantsCommand, Unit, GroupConversation>
{
    private const int MinimumParticipantsCount = 2;

    private readonly IGroupConversationWriteRepository _groupConversationRepository;

    public RemoveGroupParticipantsCommandHandler(
        IGroupConversationWriteRepository groupConversationRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<RemoveGroupParticipantsCommand, GroupConversation>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _groupConversationRepository = groupConversationRepository ?? throw new ArgumentNullException(nameof(groupConversationRepository));
    }

    protected override async Task<FlowChatResult<GroupConversation?>> FetchAggregateRootAsync(
        RemoveGroupParticipantsCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = await _groupConversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
            return FlowChatResult<GroupConversation?>.Failure(DomainError.NotFound("Conversation not found."));

        return FlowChatResult<GroupConversation?>.Success(conversation);
    }

    protected override Task<FlowChatResult<Unit>> ExecuteAsync(
        RemoveGroupParticipantsCommand request,
        CancellationToken cancellationToken)
    {
        var participantUserIdsToRemove = request.ParticipantUserIds
            .Select(Id<UserProfileMarker>.FromGuid)
            .Distinct()
            .ToList();

        if (participantUserIdsToRemove.Any(participantUserId => AggregateRoot!.Participants.All(p => p.UserId != participantUserId)))
        {
            return Task.FromResult(FlowChatResult<Unit>.Failure(
                DomainError.BadRequest("User is not a participant in this conversation.")));
        }

        if (AggregateRoot!.Participants.Count - participantUserIdsToRemove.Count < MinimumParticipantsCount)
        {
            return Task.FromResult(FlowChatResult<Unit>.Failure(
                DomainError.BadRequest("Group conversations must have at least two participants.")));
        }

        AggregateRoot.RemoveParticipants(participantUserIdsToRemove);

        SetUpdated();

        return Task.FromResult(FlowChatResult<Unit>.Success(Unit.Value));
    }
}
