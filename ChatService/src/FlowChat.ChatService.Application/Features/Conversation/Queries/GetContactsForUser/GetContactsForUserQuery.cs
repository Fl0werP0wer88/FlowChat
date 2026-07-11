using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetContactsForUser;

public sealed record GetContactsForUserQuery(
    Guid RequestingUserId) : IQuery<IReadOnlyCollection<ContactDto>>;
