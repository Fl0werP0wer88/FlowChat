using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishGroupConversationParticipantsRemoved;

public sealed record PublishGroupConversationParticipantsRemovedCommand(
    Guid ConversationId,
    IReadOnlyCollection<Guid> ParticipantUserIds) : ICommand<Unit>;
