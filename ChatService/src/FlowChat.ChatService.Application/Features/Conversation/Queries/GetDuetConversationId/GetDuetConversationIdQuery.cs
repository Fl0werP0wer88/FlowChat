using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversationId;

public sealed record GetDuetConversationIdQuery(Guid RequestingUserId, Guid PartnerUserId) : IQuery<Guid>;
