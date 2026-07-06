using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversationsForContacts;

public sealed record GetDuetConversationsForContactsQuery(
    Guid RequestingUserId,
    IReadOnlyList<Guid> PartnerUserIds) : IQuery<IReadOnlyCollection<DuetConversationForContactDto>>;
