using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.AddGroupParticipants;

public sealed record AddGroupParticipantsCommand(
    Guid ConversationId,
    IReadOnlyList<Guid> ParticipantUserIds) : ICommand<bool>;
