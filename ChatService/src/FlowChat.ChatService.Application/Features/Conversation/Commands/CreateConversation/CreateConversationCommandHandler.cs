using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateConversation;

public sealed class CreateConversationCommandHandler
    : CommandHandlerBase<CreateConversationCommand, Guid>
{
    private readonly IConversationWriteRepository _conversationRepository;
    private ConversationAggregate? _conversation;

    public CreateConversationCommandHandler(
        IConversationWriteRepository conversationRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
        : base(domainEventDispatcher, unitOfWork)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
        CreateConversationCommand request,
        CancellationToken cancellationToken)
    {
        _conversation = ConversationAggregate.Create(
            request.IsGroup,
            request.CreatedByUserId,
            request.ParticipantUserIds,
            request.Name);

        await _conversationRepository.AddAsync(_conversation, cancellationToken);

        return FlowChatResult<Guid>.Success(_conversation.Id.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Guid> result) =>
        result.IsSuccess ? _conversation : null;
}
