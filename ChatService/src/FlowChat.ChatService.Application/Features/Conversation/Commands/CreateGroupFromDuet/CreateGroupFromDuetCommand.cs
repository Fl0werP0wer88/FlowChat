using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupFromDuet;

public sealed record CreateGroupFromDuetCommand(
    Guid RequestingUserId,
    Guid PartnerUserId) : ICommand<GroupConversationDetailDto>;
