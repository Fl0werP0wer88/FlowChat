using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationMembershipDeltaV2;

public sealed record RouteConversationMembershipDeltaV2Command(
    Guid ConversationId,
    int ProjectionRevision,
    IReadOnlyCollection<ConversationMembershipDeltaItemV2> Delta) : ICommand<Unit>;
