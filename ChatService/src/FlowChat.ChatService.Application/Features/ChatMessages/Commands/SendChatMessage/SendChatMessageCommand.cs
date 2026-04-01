using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.ChatMessages.Commands.SendChatMessage;

public sealed record SendChatMessageCommand(
    Guid ConversationId,
    Guid SenderUserId,
    string? SenderDisplayName,
    string? Text,
    IReadOnlyCollection<Guid> RecipientUserIds) : ICommand<Guid>;

