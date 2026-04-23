using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.GetOrCreateDuetConversation;

public sealed record GetOrCreateDuetConversationCommand(
    Guid Id,
    Guid RequestingUserId,
    Guid PartnerUserId) : ICommand<DuetConversationDetailDto>;
