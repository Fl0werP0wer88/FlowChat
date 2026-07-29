using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Queries.CatchUpConversationMessages;

public sealed class CatchUpConversationMessagesQueryHandler
    : IQueryHandler<CatchUpConversationMessagesQuery, ConversationMessagesCatchUpPageDto>
{
    private readonly IChatMessageReadRepository _chatMessageReadRepository;
    private readonly IConversationMessageSequenceReadRepository _sequenceReadRepository;
    private readonly IConversationParticipantReadRepository _participantReadRepository;

    public CatchUpConversationMessagesQueryHandler(
        IChatMessageReadRepository chatMessageReadRepository,
        IConversationMessageSequenceReadRepository sequenceReadRepository,
        IConversationParticipantReadRepository participantReadRepository)
    {
        _chatMessageReadRepository = chatMessageReadRepository ?? throw new ArgumentNullException(nameof(chatMessageReadRepository));
        _sequenceReadRepository = sequenceReadRepository ?? throw new ArgumentNullException(nameof(sequenceReadRepository));
        _participantReadRepository = participantReadRepository ?? throw new ArgumentNullException(nameof(participantReadRepository));
    }

    public async Task<FlowChatResult<ConversationMessagesCatchUpPageDto>> Handle(
        CatchUpConversationMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var participantUserIds = await _participantReadRepository.GetParticipantUserIdsAsync(
            request.ConversationId,
            cancellationToken);
        if (participantUserIds is null)
        {
            return FlowChatResult<ConversationMessagesCatchUpPageDto>.Failure(
                DomainError.NotFound("Conversation not found."));
        }

        if (!participantUserIds.Contains(request.RequestingUserId))
        {
            return FlowChatResult<ConversationMessagesCatchUpPageDto>.Failure(
                DomainError.Unauthorized("Requesting user is not a participant of this conversation."));
        }

        var currentSequenceNum = await _sequenceReadRepository.GetCurrentAsync(
            request.ConversationId,
            cancellationToken) ?? 0;
        var throughSequenceNum = request.ThroughSequenceNum ?? currentSequenceNum;

        if (throughSequenceNum < request.AfterSequenceNum)
        {
            return FlowChatResult<ConversationMessagesCatchUpPageDto>.Failure(
                DomainError.BadRequest("ThroughSequenceNum cannot be lower than AfterSequenceNum."));
        }

        if (throughSequenceNum > currentSequenceNum)
        {
            return FlowChatResult<ConversationMessagesCatchUpPageDto>.Failure(
                DomainError.BadRequest(
                    "ThroughSequenceNum cannot exceed the current conversation sequence."));
        }

        var rows = await _chatMessageReadRepository.GetAfterSequenceAsync(
            request.ConversationId,
            request.AfterSequenceNum,
            throughSequenceNum,
            request.Limit + 1,
            cancellationToken);
        var hasMore = rows.Count > request.Limit;
        var items = rows.Take(request.Limit).ToList();

        return FlowChatResult<ConversationMessagesCatchUpPageDto>.Success(
            new ConversationMessagesCatchUpPageDto(
                items,
                hasMore ? items[^1].SequenceNum : null,
                currentSequenceNum,
                throughSequenceNum,
                hasMore));
    }
}
