using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationParticipantsAdded;

public sealed record RouteGroupConversationParticipantsAddedCommand(
    Guid ConversationId,
    IReadOnlyCollection<Guid> ParticipantUserIds,
    int ConversationVersion) : ICommand<Unit>;
