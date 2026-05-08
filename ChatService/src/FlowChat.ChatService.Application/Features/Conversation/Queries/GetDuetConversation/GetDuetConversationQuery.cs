using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversation;

public sealed record GetDuetConversationQuery(
    Guid RequestingUserId,
    Guid PartnerUserId) : IQuery<DuetConversationDetailDto>;
