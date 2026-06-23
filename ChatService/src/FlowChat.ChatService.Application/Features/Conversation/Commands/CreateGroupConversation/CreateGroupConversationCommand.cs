using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;

public sealed record CreateGroupConversationCommand(
    Guid ConversationId,
    Guid CreatedByUserId,
    IReadOnlyCollection<Guid> ParticipantUserIds,
    string Name) : ICommand<IdempotentCommandResult<GroupConversationDetailDto>>
{
    public const string IdempotencyConflictKey = nameof(CreateGroupConversationCommand);
}
