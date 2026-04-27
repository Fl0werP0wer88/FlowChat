using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;

public sealed class CreateGroupConversationCommandHandler
    : CommandHandlerBase<CreateGroupConversationCommand, Guid>
{
    private readonly IGroupConversationWriteRepository _conversationRepository;
    private GroupConversation? _conversation;

    public CreateGroupConversationCommandHandler(
        IGroupConversationWriteRepository conversationRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
        : base(domainEventDispatcher, unitOfWork)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
        CreateGroupConversationCommand request,
        CancellationToken cancellationToken)
    {
        _conversation = GroupConversation.Create(
            request.CreatedByUserId,
            request.ParticipantUserIds,
            request.Name);

        await _conversationRepository.AddAsync(_conversation, cancellationToken);

        return FlowChatResult<Guid>.Success(_conversation.Id.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Guid> result) =>
        result.IsSuccess ? _conversation : null;
}
