using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateConversation;

public sealed record CreateConversationCommand(
    Guid Id,
    ConversationType Type,
    Guid CreatedByUserId,
    IReadOnlyCollection<Guid> ParticipantUserIds,
    string? Name) : ICommand<Guid>;
