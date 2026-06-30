using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.AddParticipant;

public sealed record AddParticipantCommand(
    Guid ConversationId,
    IReadOnlyList<Guid> ParticipantUserIds) : ICommand<bool>;
