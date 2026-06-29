using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishConversationChanged;

public sealed record PublishConversationChangedCommand(
    Guid ConversationId,
    int Type,
    string? Name,
    Guid CreatedByUserId,
    IReadOnlyCollection<Guid> ParticipantUserIds) : ICommand<Unit>;
