using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Public.GetConversationMessages;

public sealed record GetConversationMessagesResponse(
    IReadOnlyCollection<ChatMessageResponse> Items,
    DateTimeOffset? NextBeforeSentAtUtc,
    Guid? NextBeforeMessageId,
    bool HasMore) : IServiceOutput;

public sealed record ChatMessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string SenderDisplayName,
    string Text,
    DateTimeOffset SentAtUtc);
