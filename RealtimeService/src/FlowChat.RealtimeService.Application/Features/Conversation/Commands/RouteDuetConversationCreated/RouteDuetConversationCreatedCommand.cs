using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteDuetConversationCreated;

public sealed record RouteDuetConversationCreatedCommand(
    Guid ConversationId,
    IReadOnlyCollection<Guid> ParticipantUserIds,
    int ConversationMembershipRevision) : ICommand<Unit>;
