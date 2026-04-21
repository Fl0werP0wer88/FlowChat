using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.GetOrCreateDuetConversation;

public sealed class GetOrCreateDuetConversationCommandHandler
    : CommandHandlerBase<GetOrCreateDuetConversationCommand, DuetConversationDetailDto>
{
    private readonly IConversationWriteRepository _conversationRepository;
    private readonly IDuetConversationRepository _duetConversationRepository;
    private readonly IUserProfileProjectionReadRepository _profileReadRepository;
    private ConversationAggregate? _newConversation;

    public GetOrCreateDuetConversationCommandHandler(
        IConversationWriteRepository conversationRepository,
        IDuetConversationRepository duetConversationRepository,
        IUserProfileProjectionReadRepository profileReadRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
        : base(domainEventDispatcher, unitOfWork)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _duetConversationRepository = duetConversationRepository ?? throw new ArgumentNullException(nameof(duetConversationRepository));
        _profileReadRepository = profileReadRepository ?? throw new ArgumentNullException(nameof(profileReadRepository));
    }

    protected override async Task<FlowChatResult<DuetConversationDetailDto>> ExecuteAsync(
        GetOrCreateDuetConversationCommand request,
        CancellationToken cancellationToken)
    {
        var existingConversationId = await _duetConversationRepository.FindConversationIdAsync(
            request.RequestingUserId, request.PartnerUserId, cancellationToken);

        Guid conversationId;
        IReadOnlyCollection<Guid> participantUserIds;

        if (existingConversationId.HasValue)
        {
            var existing = await _conversationRepository.GetByIdAsync(
                existingConversationId.Value, cancellationToken);

            if (existing is null)
                return FlowChatResult<DuetConversationDetailDto>.Failure(
                    DomainError.NotFound("Conversation not found."));

            conversationId = existing.Id.Value;
            participantUserIds = [.. existing.Participants.Select(p => p.UserId)];
        }
        else
        {
            _newConversation = ConversationAggregate.Create(
                isGroup: false,
                createdByUserId: request.RequestingUserId,
                participantUserIds: [request.RequestingUserId, request.PartnerUserId],
                name: null);

            await _conversationRepository.AddAsync(_newConversation, cancellationToken);

            await _duetConversationRepository.AddAsync(
                request.RequestingUserId, request.PartnerUserId, _newConversation.Id.Value,
                cancellationToken);

            conversationId = _newConversation.Id.Value;
            participantUserIds = [request.RequestingUserId, request.PartnerUserId];
        }

        var profiles = await _profileReadRepository.GetByIdsAsync(participantUserIds, cancellationToken);

        var participantDtos = participantUserIds
            .Select(userId =>
            {
                var profile = profiles.FirstOrDefault(p => p.UserId == userId);
                return new ConversationParticipantDto(
                    userId,
                    profile?.DisplayName,
                    profile?.AvatarUrl,
                    profile?.FriendlyUserId ?? string.Empty);
            })
            .ToList();

        return FlowChatResult<DuetConversationDetailDto>.Success(
            new DuetConversationDetailDto(conversationId, participantDtos));
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<DuetConversationDetailDto> result) =>
        result.IsSuccess ? _newConversation : null;
}
