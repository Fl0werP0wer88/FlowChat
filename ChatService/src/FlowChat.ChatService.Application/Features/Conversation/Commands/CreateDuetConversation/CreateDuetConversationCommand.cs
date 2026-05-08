using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;

public sealed record CreateDuetConversationCommand(
    Guid RequestingUserId,
    Guid PartnerUserId) : ICommand<IdempotentCommandResult<DuetConversationDetailDto>>
{
    public const string IdempotencyConflictKey = nameof(CreateDuetConversationCommand);
}
