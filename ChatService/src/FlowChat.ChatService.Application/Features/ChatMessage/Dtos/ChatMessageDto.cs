using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Dtos;

public sealed record ChatMessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string Text,
    DateTimeOffset SentAtUtc) : IDbReadResponse;
