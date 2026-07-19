using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.ConversationV2;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupFromDuet;

//Todo: Wydaje mi sie ze więcej sensu bedzie miało pobierania duet conversation na podstawie conversationId a nie id uzytkowników. Będziemy wtedy mogli pozbyc się jednej tabeli
public sealed class CreateGroupFromDuetCommandHandlerV2
    : AggregateRootInsertCommandHandlerBaseV3<
        CreateGroupFromDuetCommandV2,
        GroupConversationDetailDto,
        ConversationAggregate>
{
    private readonly IConversationV2WriteRepository _conversationRepository;
    private readonly IConversationParticipantWriteRepository _participantRepository;
    private readonly IUserProfileProjectionReadRepository _profileRepository;
    private ConversationAggregate? _conversation;

    public CreateGroupFromDuetCommandHandlerV2(
        IConversationV2WriteRepository conversationRepository,
        IConversationParticipantWriteRepository participantRepository,
        IUserProfileProjectionReadRepository profileRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher localEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<CreateGroupFromDuetCommandV2, ConversationAggregate>> processors)
        : base(localEventDispatcher, unitOfWork, processors)
    {
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _profileRepository = profileRepository;
    }

    protected override async Task<FlowChatResult<AggregateMutation<GroupConversationDetailDto>>> ExecuteAsync(
        CreateGroupFromDuetCommandV2 request,
        CancellationToken cancellationToken)
    {
        var requestingUserId = Id<UserProfileMarker>.FromGuid(request.RequestingUserId);
        var partnerUserId = Id<UserProfileMarker>.FromGuid(request.PartnerUserId);
        var duet = await _conversationRepository.GetDuetByUserIdsAsync(
            requestingUserId,
            partnerUserId,
            cancellationToken);

        if (duet is null)
        {
            return Failure(
                DomainError.NotFound("Duet conversation not found."));
        }

        var duetParticipants = await _participantRepository.GetActiveByConversationIdAsync(
            duet.Id,
            cancellationToken);
        var expectedIds = new[] { requestingUserId, partnerUserId };
        if (duetParticipants.Count != 2 ||
            expectedIds.Any(id => duetParticipants.All(participant => participant.UserId != id)))
        {
            return Failure(
                DomainError.NotFound("Duet conversation participants were not found."));
        }

        var firstName = duetParticipants.First(x => x.UserId == requestingUserId).DisplayName
            ?? request.RequestingUserId.ToString("D");
        var secondName = duetParticipants.First(x => x.UserId == partnerUserId).DisplayName
            ?? request.PartnerUserId.ToString("D");

        _conversation = ConversationAggregate.CreateGroup(
            Id<ConversationAggregate>.FromGuid(request.NewGroupConversationId),
            requestingUserId,
            expectedIds,
            $"{firstName}/{secondName}");

        await _conversationRepository.AddAsync(_conversation, expectedIds, cancellationToken);

        var profiles = await _profileRepository.GetByIdsAsync(
            expectedIds.Select(x => x.Value).ToList(),
            cancellationToken);
        var participants = expectedIds
            .Select(id =>
            {
                var state = duetParticipants.First(x => x.UserId == id);
                var profile = profiles.FirstOrDefault(x => x.UserId == id.Value);
                return new ConversationParticipantDto(
                    id.Value,
                    state.DisplayName ?? profile?.DisplayName,
                    profile?.AvatarUrl,
                    id.Value);
            })
            .ToArray();

        return Created(
            new GroupConversationDetailDto(_conversation.Id.Value, _conversation.Name!, participants));
    }

    protected override ConversationAggregate GetAggregateRoot() =>
        _conversation ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
