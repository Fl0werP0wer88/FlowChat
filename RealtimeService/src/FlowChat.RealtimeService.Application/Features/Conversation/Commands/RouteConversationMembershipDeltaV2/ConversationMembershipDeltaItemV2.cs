using FlowChat.Core.Messaging;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationMembershipDeltaV2;

public sealed record ConversationMembershipDeltaItemV2(
    Guid ParticipantUserId,
    OperationType Operation);
