using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateConversation;

public sealed record CreateConversationCommand(
    bool IsGroup,
    Guid CreatedByUserId,
    IReadOnlyCollection<Guid> ParticipantUserIds,
    string? Name) : ICommand<Guid>;
