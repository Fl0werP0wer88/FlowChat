using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetDuetConversationIds;

public sealed record GetDuetConversationIdsRequest(IReadOnlyList<Guid> PartnerUserIds) : IServiceInput;
