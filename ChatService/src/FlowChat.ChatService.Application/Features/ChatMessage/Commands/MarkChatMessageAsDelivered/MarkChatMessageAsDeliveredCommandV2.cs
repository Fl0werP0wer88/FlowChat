using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.MarkChatMessageAsDelivered;

public sealed record MarkChatMessageAsDeliveredCommandV2(
    Guid MessageId,
    Guid ConversationId,
    DateTimeOffset DeliveredAtUtc) : ICommand<Unit>;
