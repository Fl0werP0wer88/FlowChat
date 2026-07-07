using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishGroupConversationParticipantsAdded;

public sealed record PublishGroupConversationParticipantsAddedCommand(
    Guid ConversationId,
    IReadOnlyCollection<Guid> ParticipantUserIds) : ICommand<Unit>;
