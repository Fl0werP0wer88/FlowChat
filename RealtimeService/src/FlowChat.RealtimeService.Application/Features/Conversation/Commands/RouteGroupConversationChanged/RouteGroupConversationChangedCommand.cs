using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationChanged;

public sealed record RouteGroupConversationChangedCommand(
    Guid ConversationId,
    int Type,
    string? Name,
    Guid CreatedByUserId,
    IReadOnlyCollection<Guid> ParticipantUserIds) : ICommand<Unit>;
