using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using GroupConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.GroupConversation;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;
using ParticipantUser = FlowChat.ChatService.Domain.Entities.Conversation.ParticipantUser;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;

public sealed class CreateGroupConversationCommandHandler
    : AggregateRootInsertCommandHandlerBaseV3<CreateGroupConversationCommand, GroupConversationDetailDto, GroupConversationAggregate>
{
    private readonly IGroupConversationWriteRepository _conversationWriteRepository;
    private readonly IUserProfileProjectionReadRepository _profileReadRepository;
    private readonly IConversationMessageSequenceRepository _sequenceRepository;
    private GroupConversationAggregate? _conversation;

    public CreateGroupConversationCommandHandler(
        IGroupConversationWriteRepository conversationWriteRepository,
        IUserProfileProjectionReadRepository profileReadRepository,
        IConversationMessageSequenceRepository sequenceRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<CreateGroupConversationCommand, GroupConversationAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _conversationWriteRepository = conversationWriteRepository ?? throw new ArgumentNullException(nameof(conversationWriteRepository));
        _profileReadRepository = profileReadRepository ?? throw new ArgumentNullException(nameof(profileReadRepository));
        _sequenceRepository = sequenceRepository ?? throw new ArgumentNullException(nameof(sequenceRepository));
    }

    protected override async Task<FlowChatResult<GroupConversationDetailDto>> ExecuteAsync(
        CreateGroupConversationCommand request,
        CancellationToken cancellationToken)
    {
        _conversation = GroupConversationAggregate.Create(
            Id<ConversationAggregate>.FromGuid(request.ConversationId),
            Id<UserProfileMarker>.FromGuid(request.CreatedByUserId),
            request.ParticipantUserIds.Select(Id<UserProfileMarker>.FromGuid),
            request.Name);

        await _conversationWriteRepository.AddAsync(_conversation, cancellationToken);
        await _sequenceRepository.AddAsync(_conversation.Id.Value, cancellationToken);
        SetInserted();

        var participantUserIds = _conversation.Participants.Select(p => p.UserId.Value).ToList();
        var profiles = await _profileReadRepository.GetByIdsAsync(participantUserIds, cancellationToken);

        var participantDtos = _conversation.Participants
            .Select(participant =>
            {
                var profile = profiles.FirstOrDefault(p => p.UserId == participant.UserId.Value);
                return BuildParticipantDto(participant, profile);
            })
            .ToList();

        return FlowChatResult<GroupConversationDetailDto>.Success(
            new GroupConversationDetailDto(_conversation.Id.Value, _conversation.Name!, participantDtos));
    }

    protected override GroupConversationAggregate GetAggregateRoot() =>
        _conversation ?? throw new InvalidOperationException("Aggregate root instance is not available.");

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
}
