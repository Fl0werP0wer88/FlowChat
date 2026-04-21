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
    private readonly IDuetConversationReadRepository _duetConversationReadRepository;
    private readonly IDuetConversationWriteRepository _duetConversationWriteRepository;
    private readonly IUserProfileProjectionReadRepository _profileReadRepository;
    private ConversationAggregate? _newConversation;

    public GetOrCreateDuetConversationCommandHandler(
        IConversationWriteRepository conversationRepository,
        IDuetConversationReadRepository duetConversationReadRepository,
        IDuetConversationWriteRepository duetConversationWriteRepository,
        IUserProfileProjectionReadRepository profileReadRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
        : base(domainEventDispatcher, unitOfWork)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _duetConversationReadRepository = duetConversationReadRepository ?? throw new ArgumentNullException(nameof(duetConversationReadRepository));
        _duetConversationWriteRepository = duetConversationWriteRepository ?? throw new ArgumentNullException(nameof(duetConversationWriteRepository));
        _profileReadRepository = profileReadRepository ?? throw new ArgumentNullException(nameof(profileReadRepository));
    }

    protected override async Task<FlowChatResult<DuetConversationDetailDto>> ExecuteAsync(
        GetOrCreateDuetConversationCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _duetConversationReadRepository.GetByUserIdsAsync(
            request.RequestingUserId,
            request.PartnerUserId,
            cancellationToken);

        if (existing is not null)
        {
            return FlowChatResult<DuetConversationDetailDto>.Success(existing);
        }

        var existingConversationId = await _duetConversationReadRepository.FindConversationIdAsync(
            request.RequestingUserId,
            request.PartnerUserId,
            cancellationToken);

        if (existingConversationId.HasValue)
        {
            return FlowChatResult<DuetConversationDetailDto>.Failure(
                DomainError.NotFound("Conversation not found."));
        }

        _newConversation = ConversationAggregate.Create(
            isGroup: false,
            createdByUserId: request.RequestingUserId,
            participantUserIds: [request.RequestingUserId, request.PartnerUserId],
            name: null);

        await _conversationRepository.AddAsync(_newConversation, cancellationToken);

        await _duetConversationWriteRepository.AddAsync(
            request.RequestingUserId,
            request.PartnerUserId,
            _newConversation.Id.Value,
            cancellationToken);

        IReadOnlyCollection<Guid> participantUserIds = [request.RequestingUserId, request.PartnerUserId];

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
            new DuetConversationDetailDto(_newConversation.Id.Value, participantDtos));
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<DuetConversationDetailDto> result) =>
        result.IsSuccess ? _newConversation : null;
}
