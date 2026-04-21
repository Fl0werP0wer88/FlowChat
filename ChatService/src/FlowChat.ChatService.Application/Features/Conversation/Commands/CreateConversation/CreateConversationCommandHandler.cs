using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateConversation;

public sealed class CreateConversationCommandHandler
    : CommandHandlerBase<CreateConversationCommand, Guid>
{
    private readonly IConversationWriteRepository _conversationRepository;
    private readonly IDuetConversationReadRepository _duetConversationReadRepository;
    private readonly IDuetConversationWriteRepository _duetConversationWriteRepository;
    private ConversationAggregate? _conversation;

    public CreateConversationCommandHandler(
        IConversationWriteRepository conversationRepository,
        IDuetConversationReadRepository duetConversationReadRepository,
        IDuetConversationWriteRepository duetConversationWriteRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
        : base(domainEventDispatcher, unitOfWork)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _duetConversationReadRepository = duetConversationReadRepository ?? throw new ArgumentNullException(nameof(duetConversationReadRepository));
        _duetConversationWriteRepository = duetConversationWriteRepository ?? throw new ArgumentNullException(nameof(duetConversationWriteRepository));
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
        CreateConversationCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.IsGroup)
        {
            var participants = request.ParticipantUserIds.ToList();
            var userId1 = participants[0];
            var userId2 = participants[1];

            var existingConversationId = await _duetConversationReadRepository.FindConversationIdAsync(
                userId1, userId2, cancellationToken);

            if (existingConversationId.HasValue)
                return FlowChatResult<Guid>.Success(existingConversationId.Value);
        }

        _conversation = ConversationAggregate.Create(
            request.IsGroup,
            request.CreatedByUserId,
            request.ParticipantUserIds,
            request.Name);

        await _conversationRepository.AddAsync(_conversation, cancellationToken);

        if (!request.IsGroup)
        {
            var participants = request.ParticipantUserIds.ToList();
            await _duetConversationWriteRepository.AddAsync(
                participants[0], participants[1], _conversation.Id.Value,
                cancellationToken);
        }

        return FlowChatResult<Guid>.Success(_conversation.Id.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Guid> result) =>
        result.IsSuccess ? _conversation : null;
}
