using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationChanged;

public sealed record RouteConversationChangedCommand(
    Guid ConversationId,
    int Type,
    string? Name,
    Guid CreatedByUserId,
    IReadOnlyCollection<Guid> ParticipantUserIds) : ICommand<Unit>;
