using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishConversationParticipantsAdded;

public sealed record PublishConversationParticipantsAddedCommand(
    Guid ConversationId,
    int ConversationType,
    IReadOnlyCollection<Guid> ParticipantUserIds) : ICommand<Unit>;
