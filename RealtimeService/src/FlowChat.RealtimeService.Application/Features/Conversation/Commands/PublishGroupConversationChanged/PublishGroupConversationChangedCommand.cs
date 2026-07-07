using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishGroupConversationChanged;

public sealed record PublishGroupConversationChangedCommand(
    Guid ConversationId,
    int Type,
    string? Name,
    Guid CreatedByUserId) : ICommand<Unit>;
