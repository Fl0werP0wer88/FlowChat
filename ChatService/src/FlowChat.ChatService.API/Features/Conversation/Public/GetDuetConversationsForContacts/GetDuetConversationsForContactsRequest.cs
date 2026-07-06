using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetDuetConversationsForContacts;

public sealed record GetDuetConversationsForContactsRequest(IReadOnlyList<Guid> PartnerUserIds) : IServiceInput;
