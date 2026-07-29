using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Public.CatchUpConversationMessages;

public sealed record CatchUpConversationMessagesResponse(
    IReadOnlyCollection<ChatMessageResponse> Items,
    long? NextAfterSequenceNum,
    long CurrentSequenceNum,
    long ThroughSequenceNum,
    bool HasMore) : IServiceOutput;

public sealed record ChatMessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string Text,
    DateTimeOffset SentAtUtc,
    long SequenceNum);
