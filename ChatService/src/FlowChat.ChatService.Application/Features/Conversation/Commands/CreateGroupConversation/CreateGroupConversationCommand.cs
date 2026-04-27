using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;

public sealed record CreateGroupConversationCommand(
    Guid CreatedByUserId,
    IReadOnlyCollection<Guid> ParticipantUserIds,
    string Name) : ICommand<Guid>;
