using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using GroupConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.GroupConversation;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;
using ParticipantUser = FlowChat.ChatService.Domain.Entities.Conversation.ParticipantUser;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupFromDuet;

public sealed class CreateGroupFromDuetCommandHandler
    : AggregateRootInsertCommandHandlerBaseV3<CreateGroupFromDuetCommand, GroupConversationDetailDto, GroupConversationAggregate>
{
    private readonly IDuetConversationReadRepository _duetReadRepository;
    private readonly IGroupConversationWriteRepository _groupWriteRepository;
    private readonly IUserProfileProjectionReadRepository _profileReadRepository;
    private GroupConversationAggregate? _conversation;

    public CreateGroupFromDuetCommandHandler(
        IDuetConversationReadRepository duetReadRepository,
        IGroupConversationWriteRepository groupWriteRepository,
        IUserProfileProjectionReadRepository profileReadRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<CreateGroupFromDuetCommand, GroupConversationAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _duetReadRepository = duetReadRepository ?? throw new ArgumentNullException(nameof(duetReadRepository));
        _groupWriteRepository = groupWriteRepository ?? throw new ArgumentNullException(nameof(groupWriteRepository));
        _profileReadRepository = profileReadRepository ?? throw new ArgumentNullException(nameof(profileReadRepository));
    }

    protected override async Task<FlowChatResult<AggregateMutation<GroupConversationDetailDto>>> ExecuteAsync(
        CreateGroupFromDuetCommand request,
        CancellationToken cancellationToken)
    {
        var duet = await _duetReadRepository.GetByUserIdsAsync(
            request.RequestingUserId,
            request.PartnerUserId,
            cancellationToken);

        if (duet is null)
        {
            return Failure(
                DomainError.NotFound("Duet conversation not found."));
        }

        var user1Name = duet.Participants
            .FirstOrDefault(p => p.UserId == request.RequestingUserId)?.DisplayName
            ?? request.RequestingUserId.ToString("D");

        var user2Name = duet.Participants
            .FirstOrDefault(p => p.UserId == request.PartnerUserId)?.DisplayName
            ?? request.PartnerUserId.ToString("D");

        _conversation = GroupConversationAggregate.Create(
            Id<ConversationAggregate>.FromGuid(request.NewGroupConversationId),
            Id<UserProfileMarker>.FromGuid(request.RequestingUserId),
            [
                Id<UserProfileMarker>.FromGuid(request.RequestingUserId),
                Id<UserProfileMarker>.FromGuid(request.PartnerUserId)
            ],
            $"{user1Name}/{user2Name}");

        await _groupWriteRepository.AddAsync(_conversation, cancellationToken);

        var participantUserIds = _conversation.Participants.Select(p => p.UserId.Value).ToList();
        var profiles = await _profileReadRepository.GetByIdsAsync(participantUserIds, cancellationToken);

        var participantDtos = _conversation.Participants
            .Select(participant =>
            {
                var profile = profiles.FirstOrDefault(p => p.UserId == participant.UserId.Value);
                return BuildParticipantDto(participant, profile);
            })
            .ToList();

        return Created(
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
