using FlowChat.Core.Contracts;

namespace FlowChat.GatewayService.Api.Features.ChatMessage.Public.GetConversationMessages;

public sealed record GetConversationMessagesResponse(
    IReadOnlyCollection<ChatMessageResponse> Items,
    long? NextBeforeSequenceNum,
    long CurrentSequenceNum,
    bool HasMore) : IServiceOutput;

public sealed record ChatMessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string Text,
    DateTimeOffset SentAtUtc,
    long SequenceNum);
