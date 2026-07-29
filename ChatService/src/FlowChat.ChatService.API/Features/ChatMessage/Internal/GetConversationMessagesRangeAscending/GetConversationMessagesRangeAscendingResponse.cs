using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Internal.GetConversationMessagesRangeAscending;

public sealed record GetConversationMessagesRangeAscendingResponse(
    IReadOnlyCollection<ChatMessageResponse> Items,
    long StartSequenceNum,
    long EndSequenceNum,
    long CurrentSequenceNum,
    bool HasMore) : IServiceOutput;

public sealed record ChatMessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string Text,
    DateTimeOffset SentAtUtc,
    long SequenceNum);
