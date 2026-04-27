using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;

public sealed record CreateDuetConversationCommand(
    Guid RequestingUserId,
    Guid PartnerUserId) : ICommand<CreateDuetConversationResult>;
