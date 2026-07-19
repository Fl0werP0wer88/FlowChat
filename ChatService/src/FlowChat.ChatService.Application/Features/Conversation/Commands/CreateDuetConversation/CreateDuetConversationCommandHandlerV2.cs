using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.ConversationV2;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;

public sealed class CreateDuetConversationCommandHandlerV2
    : AggregateRootInsertCommandHandlerBaseV3<
        CreateDuetConversationCommandV2,
        DuetConversationDetailDto,
        ConversationAggregate>
{
    private readonly IConversationV2WriteRepository _conversationRepository;
    private readonly IUserProfileProjectionReadRepository _profileRepository;
    private ConversationAggregate? _conversation;

    public CreateDuetConversationCommandHandlerV2(
        IConversationV2WriteRepository conversationRepository,
        IUserProfileProjectionReadRepository profileRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher localEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<CreateDuetConversationCommandV2, ConversationAggregate>> processors)
        : base(localEventDispatcher, unitOfWork, processors)
    {
        _conversationRepository = conversationRepository;
        _profileRepository = profileRepository;
    }

    protected override async Task<FlowChatResult<DuetConversationDetailDto>> ExecuteAsync(
        CreateDuetConversationCommandV2 request,
        CancellationToken cancellationToken)
    {
        var requestingUserId = Id<UserProfileMarker>.FromGuid(request.RequestingUserId);
        var partnerUserId = Id<UserProfileMarker>.FromGuid(request.PartnerUserId);

        if (await _conversationRepository.GetDuetByUserIdsAsync(
                requestingUserId,
                partnerUserId,
                cancellationToken) is not null)
        {
            return FlowChatResult<DuetConversationDetailDto>.Failure(
                DomainError.Conflict("Duet conversation already exists."));
        }

        var participantIds = new[] { requestingUserId, partnerUserId };
        _conversation = ConversationAggregate.CreateDuet(requestingUserId, partnerUserId);
        await _conversationRepository.AddAsync(_conversation, participantIds, cancellationToken);
        SetInserted();

        var profiles = await _profileRepository.GetByIdsAsync(
            participantIds.Select(x => x.Value).ToList(),
            cancellationToken);
        var participants = participantIds
            .Select(id =>
            {
                var profile = profiles.FirstOrDefault(x => x.UserId == id.Value);
                return new ConversationParticipantDto(id.Value, profile?.DisplayName, profile?.AvatarUrl, id.Value);
            })
            .ToArray();

        return FlowChatResult<DuetConversationDetailDto>.Success(
            new DuetConversationDetailDto(_conversation.Id.Value, participants));
    }

    protected override ConversationAggregate GetAggregateRoot() =>
        _conversation ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
