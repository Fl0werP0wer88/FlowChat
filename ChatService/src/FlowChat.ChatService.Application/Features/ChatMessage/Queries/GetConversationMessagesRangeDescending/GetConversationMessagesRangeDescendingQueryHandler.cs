using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessagesRangeDescending;

public sealed class GetConversationMessagesRangeDescendingQueryHandler
    : IQueryHandler<GetConversationMessagesRangeDescendingQuery, ConversationMessagesRangeDescendingPageDto>
{
    private readonly IChatMessageReadRepository _messages;
    private readonly IConversationMessageSequenceReadRepository _sequences;
    private readonly IConversationParticipantReadRepository _participants;

    public GetConversationMessagesRangeDescendingQueryHandler(
        IChatMessageReadRepository messages,
        IConversationMessageSequenceReadRepository sequences,
        IConversationParticipantReadRepository participants)
    {
        _messages = messages ?? throw new ArgumentNullException(nameof(messages));
        _sequences = sequences ?? throw new ArgumentNullException(nameof(sequences));
        _participants = participants ?? throw new ArgumentNullException(nameof(participants));
    }

    public async Task<FlowChatResult<ConversationMessagesRangeDescendingPageDto>> Handle(
        GetConversationMessagesRangeDescendingQuery request,
        CancellationToken cancellationToken)
    {
        var participantUserIds = await _participants.GetParticipantUserIdsAsync(
            request.ConversationId,
            cancellationToken);
        if (participantUserIds is null)
        {
            return FlowChatResult<ConversationMessagesRangeDescendingPageDto>.Failure(
                DomainError.NotFound("Conversation not found."));
        }

        if (!participantUserIds.Contains(request.RequestingUserId))
        {
            return FlowChatResult<ConversationMessagesRangeDescendingPageDto>.Failure(
                DomainError.Unauthorized("Requesting user is not a participant of this conversation."));
        }

        var currentSequenceNum = await _sequences.GetCurrentAsync(
            request.ConversationId,
            cancellationToken) ?? 0;
        var startSequenceNum = request.StartSequenceNum ?? 1;
        var endSequenceNum = Math.Min(request.EndSequenceNum ?? currentSequenceNum, currentSequenceNum);
        var rows = startSequenceNum > endSequenceNum
            ? []
            : await _messages.GetRangeDescendingAsync(
                request.ConversationId,
                startSequenceNum,
                endSequenceNum,
                request.Limit + 1,
                cancellationToken);
        var hasMore = rows.Count > request.Limit;

        return FlowChatResult<ConversationMessagesRangeDescendingPageDto>.Success(
            new ConversationMessagesRangeDescendingPageDto(
                rows.Take(request.Limit).ToList(),
                startSequenceNum,
                endSequenceNum,
                currentSequenceNum,
                hasMore));
    }
}
