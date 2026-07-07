using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationParticipantsRemoved;

public sealed record RouteGroupConversationParticipantsRemovedCommand(
    Guid ConversationId,
    IReadOnlyCollection<Guid> ParticipantUserIds) : ICommand<Unit>;
