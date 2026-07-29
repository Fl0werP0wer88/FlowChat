using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Public.GetConversationMessages;

public sealed record GetConversationMessagesResponse(
    IReadOnlyCollection<ChatMessageResponse> Items,
    long? NextBeforeSequenceNum,
    long? NextAfterSequenceNum,
    long CurrentSequenceNum,
    long? ThroughSequenceNum,
    bool HasMore) : IServiceOutput;

public sealed record ChatMessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string Text,
    DateTimeOffset SentAtUtc,
    long SequenceNum);
