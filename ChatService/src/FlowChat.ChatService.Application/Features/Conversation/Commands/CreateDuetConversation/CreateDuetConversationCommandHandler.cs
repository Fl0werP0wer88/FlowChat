using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using DuetConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;

public sealed class CreateDuetConversationCommandHandler
    : AggregateRootInsertCommandHandlerBaseV2<CreateDuetConversationCommand, DuetConversationDetailDto, DuetConversationAggregate>
{
    private readonly IDuetConversationWriteRepository _duetConversationWriteRepository;
    private readonly IUserProfileProjectionReadRepository _profileReadRepository;
    private DuetConversationAggregate? _newConversation;

    public CreateDuetConversationCommandHandler(
        IDuetConversationWriteRepository duetConversationWriteRepository,
        IUserProfileProjectionReadRepository profileReadRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<CreateDuetConversationCommand, DuetConversationAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _duetConversationWriteRepository = duetConversationWriteRepository ?? throw new ArgumentNullException(nameof(duetConversationWriteRepository));
        _profileReadRepository = profileReadRepository ?? throw new ArgumentNullException(nameof(profileReadRepository));
    }

    protected override async Task<FlowChatResult<DuetConversationDetailDto>> ExecuteAsync(
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

    protected override DuetConversationAggregate GetAggregateRoot() =>
        _newConversation ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
