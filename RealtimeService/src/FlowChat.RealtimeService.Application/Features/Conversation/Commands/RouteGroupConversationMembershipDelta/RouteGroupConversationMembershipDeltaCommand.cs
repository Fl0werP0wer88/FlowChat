using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationMembershipDelta;

public sealed record RouteGroupConversationMembershipDeltaCommand(
    Guid ConversationId,
    IReadOnlyCollection<Guid> ParticipantUserIds,
    DeltaOperationType Operation,
    int ProjectionRevision) : ICommand<Unit>;
