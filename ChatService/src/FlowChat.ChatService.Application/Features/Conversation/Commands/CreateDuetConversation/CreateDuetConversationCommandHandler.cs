using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using DuetConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;

public sealed class CreateDuetConversationCommandHandler
    : AggregateRootInsertCommandHandlerBaseV3<CreateDuetConversationCommand, DuetConversationDetailDto, DuetConversationAggregate>
{
    private readonly IDuetConversationWriteRepository _duetConversationWriteRepository;
    private readonly IUserProfileProjectionReadRepository _profileReadRepository;
    private readonly IConversationMessageSequenceRepository _sequenceRepository;
    private DuetConversationAggregate? _newConversation;

    public CreateDuetConversationCommandHandler(
        IDuetConversationWriteRepository duetConversationWriteRepository,
        IUserProfileProjectionReadRepository profileReadRepository,
        IConversationMessageSequenceRepository sequenceRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<CreateDuetConversationCommand, DuetConversationAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _duetConversationWriteRepository = duetConversationWriteRepository ?? throw new ArgumentNullException(nameof(duetConversationWriteRepository));
        _profileReadRepository = profileReadRepository ?? throw new ArgumentNullException(nameof(profileReadRepository));
        _sequenceRepository = sequenceRepository ?? throw new ArgumentNullException(nameof(sequenceRepository));
    }

    protected override async Task<FlowChatResult<AggregateMutation<DuetConversationDetailDto>>> ExecuteAsync(
        CreateDuetConversationCommand request,
        CancellationToken cancellationToken)
    {
        _newConversation = DuetConversationAggregate.Create(
            createdByUserId: Id<UserProfileMarker>.FromGuid(request.RequestingUserId),
            partnerUserId: Id<UserProfileMarker>.FromGuid(request.PartnerUserId));

        await _duetConversationWriteRepository.AddAsync(_newConversation, cancellationToken);
        await _sequenceRepository.AddAsync(_newConversation.Id.Value, cancellationToken);

        var participantUserIds = _newConversation.Participants.Select(p => p.UserId.Value).ToList();
        var profiles = await _profileReadRepository.GetByIdsAsync(participantUserIds, cancellationToken);

        var participantDtos = _newConversation.Participants
            .Select(participant =>
            {
                var profile = profiles.FirstOrDefault(p => p.UserId == participant.UserId.Value);
                return BuildParticipantDto(participant, profile);
            })
            .ToList();

        return Created(
            new DuetConversationDetailDto(_newConversation.Id.Value, participantDtos));
    }

    private static ConversationParticipantDto BuildParticipantDto(
        ParticipantUser participant,
        UserProfileConversationParticipantDto? profile)
    {
        return new ConversationParticipantDto(
            participant.UserId.Value,
            string.IsNullOrEmpty(participant.DisplayName) ? profile?.DisplayName : participant.DisplayName,
            profile?.AvatarUrl,
            participant.UserId.Value);
    }

    protected override DuetConversationAggregate GetAggregateRoot() =>
        _newConversation ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
