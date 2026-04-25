using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversationIds;

public sealed record GetDuetConversationIdsQuery(
    Guid RequestingUserId,
    IReadOnlyList<Guid> PartnerUserIds) : IQuery<IReadOnlyDictionary<Guid, Guid>>;
