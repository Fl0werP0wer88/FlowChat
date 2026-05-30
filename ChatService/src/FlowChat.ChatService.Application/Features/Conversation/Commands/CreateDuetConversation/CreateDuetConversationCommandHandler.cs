using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using DuetConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;

public sealed class CreateDuetConversationCommandHandler
    : IdempotentCommandHandlerBase<CreateDuetConversationCommand, DuetConversationDetailDto>
{
    private readonly IDuetConversationReadRepository _duetConversationReadRepository;
    private readonly IDuetConversationWriteRepository _duetConversationWriteRepository;
    private readonly IUserProfileProjectionReadRepository _profileReadRepository;
    private DuetConversationAggregate? _newConversation;

    public CreateDuetConversationCommandHandler(
        IDuetConversationReadRepository duetConversationReadRepository,
        IDuetConversationWriteRepository duetConversationWriteRepository,
        IUserProfileProjectionReadRepository profileReadRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IDbUpdateExceptionClassifier dbUpdateExceptionClassifier)
        : base(domainEventDispatcher, unitOfWork, dbUpdateExceptionClassifier)
    {
        _duetConversationReadRepository = duetConversationReadRepository ?? throw new ArgumentNullException(nameof(duetConversationReadRepository));
        _duetConversationWriteRepository = duetConversationWriteRepository ?? throw new ArgumentNullException(nameof(duetConversationWriteRepository));
        _profileReadRepository = profileReadRepository ?? throw new ArgumentNullException(nameof(profileReadRepository));
    }

    protected override async Task<FlowChatResult<DuetConversationDetailDto>> ExecuteCommandAsync(
        CreateDuetConversationCommand request,
        CancellationToken cancellationToken)
    {
        _newConversation = DuetConversationAggregate.Create(
            createdByUserId: request.RequestingUserId,
            partnerUserId: request.PartnerUserId);

        await _duetConversationWriteRepository.AddAsync(_newConversation, cancellationToken);

        var participantUserIds = _newConversation.Participants.Select(p => p.UserId).ToList();
        var profiles = await _profileReadRepository.GetByIdsAsync(participantUserIds, cancellationToken);

        var participantDtos = _newConversation.Participants
            .Select(participant =>
            {
                var profile = profiles.FirstOrDefault(p => p.UserId == participant.UserId);
                return BuildParticipantDto(participant, profile);
            })
            .ToList();

        return FlowChatResult<DuetConversationDetailDto>.Success(
            new DuetConversationDetailDto(_newConversation.Id.Value, participantDtos));
    }

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

    protected override async Task<(bool Found, DuetConversationDetailDto Value)> TryGetExistingResponseAsync(
        CreateDuetConversationCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _duetConversationReadRepository.GetByUserIdsAsync(
            request.RequestingUserId,
            request.PartnerUserId,
            cancellationToken);

        return existing is not null
            ? (true, existing)
            : (false, default!);
    }

    protected override IAggregateRoot? GetAggregateRoot() =>
        _newConversation;

    protected override string GetIdempotencyConflictKey(CreateDuetConversationCommand request) =>
        CreateDuetConversationCommand.IdempotencyConflictKey;
}
