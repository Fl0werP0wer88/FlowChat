using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishConversationParticipantsRemoved;

public sealed record PublishConversationParticipantsRemovedCommand(
    Guid ConversationId,
    int ConversationType,
    IReadOnlyCollection<Guid> ParticipantUserIds,
    int ParticipantCount,
    int MembershipRevision) : ICommand<Unit>;
