using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishDuetConversationCreated;

public sealed record PublishDuetConversationCreatedCommand(
    Guid ConversationId,
    IReadOnlyCollection<Guid> ParticipantUserIds) : ICommand<Unit>;
