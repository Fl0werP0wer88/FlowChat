using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.AddParticipant;

public sealed class AddParticipantCommandHandler
    : CommandHandlerBase<AddParticipantCommand, Unit>
{
    private readonly IGroupConversationWriteRepository _conversationRepository;
    private readonly IConversationParticipantReadRepository _participantReadRepository;
    private GroupConversation? _conversation;

    public AddParticipantCommandHandler(
        IGroupConversationWriteRepository conversationRepository,
        IConversationParticipantReadRepository participantReadRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
        : base(domainEventDispatcher, unitOfWork)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _participantReadRepository = participantReadRepository ?? throw new ArgumentNullException(nameof(participantReadRepository));
    }

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        AddParticipantCommand request,
        CancellationToken cancellationToken)
    {
        _conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (_conversation is null)
        {
            var participants = await _participantReadRepository.GetParticipantUserIdsAsync(request.ConversationId, cancellationToken);
            return participants is null
                ? FlowChatResult<Unit>.Failure(DomainError.NotFound("Conversation not found."))
                : FlowChatResult<Unit>.Failure(DomainError.BadRequest("Cannot add participants to a one-on-one conversation."));
        }

        if (_conversation.Participants.Any(p => p.UserId == request.ParticipantUserId))
            return FlowChatResult<Unit>.Failure(DomainError.Conflict("User is already a participant in this conversation."));

        _conversation.AddParticipant(request.ParticipantUserId, displayedName: null, avatarUrl: null);

        await _conversationRepository.UpdateAsync(_conversation, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Unit> result) =>
        result.IsSuccess ? _conversation : null;
}
