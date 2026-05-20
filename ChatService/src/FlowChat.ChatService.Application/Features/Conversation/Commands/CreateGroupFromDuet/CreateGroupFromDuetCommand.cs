using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupFromDuet;

public sealed record CreateGroupFromDuetCommand(
    Guid NewGroupConversationId,
    Guid RequestingUserId,
    Guid PartnerUserId) : ICommand<IdempotentCommandResult<GroupConversationDetailDto>>
{
    public const string IdempotencyConflictKey = nameof(CreateGroupFromDuetCommand);
}
