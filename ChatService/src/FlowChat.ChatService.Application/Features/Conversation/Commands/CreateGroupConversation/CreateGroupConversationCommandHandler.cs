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
    : IdempotentCommandHandlerBase<CreateGroupConversationCommand, GroupConversationDetailDto>
{
    private readonly IGroupConversationWriteRepository _conversationWriteRepository;
    private readonly IGroupConversationReadRepository _conversationReadRepository;
    private readonly IUserProfileProjectionReadRepository _profileReadRepository;
    private GroupConversationAggregate? _conversation;

    public CreateGroupConversationCommandHandler(
        IGroupConversationWriteRepository conversationWriteRepository,
        IGroupConversationReadRepository conversationReadRepository,
        IUserProfileProjectionReadRepository profileReadRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher,
        IDbUpdateExceptionClassifier dbUpdateExceptionClassifier)
        : base(domainEventDispatcher, unitOfWork, dbUpdateExceptionClassifier)
    {
        _conversationWriteRepository = conversationWriteRepository ?? throw new ArgumentNullException(nameof(conversationWriteRepository));
        _conversationReadRepository = conversationReadRepository ?? throw new ArgumentNullException(nameof(conversationReadRepository));
        _profileReadRepository = profileReadRepository ?? throw new ArgumentNullException(nameof(profileReadRepository));
    }

    protected override async Task<FlowChatResult<GroupConversationDetailDto>> ExecuteCommandAsync(
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

    protected override async Task<(bool Found, GroupConversationDetailDto Value)> TryGetExistingResponseAsync(
        CreateGroupConversationCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _conversationReadRepository.GetByIdAsync(request.ConversationId, cancellationToken);

        return existing is not null
            ? (true, existing)
            : (false, default!);
    }

    protected override IAggregateRoot? GetAggregateRoot() =>
        _conversation;

    protected override string GetIdempotencyConflictKey(CreateGroupConversationCommand request) =>
        CreateGroupConversationCommand.IdempotencyConflictKey;

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
