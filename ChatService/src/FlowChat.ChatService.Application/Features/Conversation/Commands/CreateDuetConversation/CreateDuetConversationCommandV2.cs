using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;

public sealed record CreateDuetConversationCommandV2(
    Guid RequestingUserId,
    Guid PartnerUserId) : ICommand<DuetConversationDetailDto>;
