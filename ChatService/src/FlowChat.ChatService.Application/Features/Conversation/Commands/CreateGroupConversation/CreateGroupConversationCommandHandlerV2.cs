using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.ConversationV2;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;

public sealed class CreateGroupConversationCommandHandlerV2
    : AggregateRootInsertCommandHandlerBaseV3<
        CreateGroupConversationCommandV2,
        GroupConversationDetailDto,
        ConversationAggregate>
{
    private readonly IConversationV2WriteRepository _conversationRepository;
    private readonly IUserProfileProjectionReadRepository _profileRepository;
    private ConversationAggregate? _conversation;

    public CreateGroupConversationCommandHandlerV2(
        IConversationV2WriteRepository conversationRepository,
        IUserProfileProjectionReadRepository profileRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher localEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessorV2<CreateGroupConversationCommandV2, ConversationAggregate>> processors)
        : base(localEventDispatcher, unitOfWork, processors)
    {
        _conversationRepository = conversationRepository;
        _profileRepository = profileRepository;
    }

    protected override async Task<FlowChatResult<AggregateMutation<GroupConversationDetailDto>>> ExecuteAsync(
        CreateGroupConversationCommandV2 request,
        CancellationToken cancellationToken)
    {
        var participantIds = request.ParticipantUserIds
            .Prepend(request.CreatedByUserId)
            .Distinct()
            .Select(Id<UserProfileMarker>.FromGuid)
            .ToArray();

        _conversation = ConversationAggregate.CreateGroup(
            Id<ConversationAggregate>.FromGuid(request.ConversationId),
            Id<UserProfileMarker>.FromGuid(request.CreatedByUserId),
            participantIds,
            request.Name);

        await _conversationRepository.AddAsync(_conversation, participantIds, cancellationToken);

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

        return Created(
            new GroupConversationDetailDto(_conversation.Id.Value, _conversation.Name!, participants));
    }

    protected override ConversationAggregate GetAggregateRoot() =>
        _conversation ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
