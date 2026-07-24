using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationProjectionV2;

public sealed record RouteConversationProjectionV2Command(
    Guid SourceAggregateId,
    Guid ConversationId,
    int ConversationType,
    string? Name,
    Guid CreatedByUserId,
    OperationType Operation) : ICommand<Unit>;
