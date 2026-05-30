using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using GroupConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.GroupConversation;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;
using ParticipantUser = FlowChat.ChatService.Domain.Entities.Conversation.ParticipantUser;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;

public sealed class CreateGroupConversationCommandHandler
    : AggregateRootCommandHandlerBase<CreateGroupConversationCommand, GroupConversationDetailDto>
{
    private readonly IGroupConversationWriteRepository _conversationWriteRepository;
    private readonly IUserProfileProjectionReadRepository _profileReadRepository;
    private GroupConversationAggregate? _conversation;

    public CreateGroupConversationCommandHandler(
        IGroupConversationWriteRepository conversationWriteRepository,
        IUserProfileProjectionReadRepository profileReadRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher)
        : base(domainEventDispatcher, unitOfWork)
    {
        _conversationWriteRepository = conversationWriteRepository ?? throw new ArgumentNullException(nameof(conversationWriteRepository));
        _profileReadRepository = profileReadRepository ?? throw new ArgumentNullException(nameof(profileReadRepository));
    }

    protected override async Task<FlowChatResult<GroupConversationDetailDto>> ExecuteAsync(
        CreateGroupConversationCommand request,
        CancellationToken cancellationToken)
    {
        _conversation = GroupConversationAggregate.Create(
            Id<ConversationAggregate>.FromGuid(request.ConversationId),
            request.CreatedByUserId,
            request.ParticipantUserIds,
            request.Name);

        await _conversationWriteRepository.AddAsync(_conversation, cancellationToken);

        var participantUserIds = _conversation.Participants.Select(p => p.UserId).ToList();
        var profiles = await _profileReadRepository.GetByIdsAsync(participantUserIds, cancellationToken);

        var participantDtos = _conversation.Participants
            .Select(participant =>
            {
                var profile = profiles.FirstOrDefault(p => p.UserId == participant.UserId);
                return BuildParticipantDto(participant, profile);
            })
            .ToList();

        return FlowChatResult<GroupConversationDetailDto>.Success(
            new GroupConversationDetailDto(_conversation.Id.Value, _conversation.Name!, participantDtos));
    }

    protected override IAggregateRoot? GetAggregateRoot() =>
        _conversation;

    private static ConversationParticipantDto BuildParticipantDto(
        ParticipantUser participant,
        UserProfileConversationParticipantDto? profile)
    {
        return new ConversationParticipantDto(
            participant.UserId,
            string.IsNullOrEmpty(participant.DisplayName) ? profile?.DisplayName : participant.DisplayName,
            string.IsNullOrEmpty(participant.AvatarUrl) ? profile?.AvatarUrl : participant.AvatarUrl,
            participant.UserId);
    }
}
