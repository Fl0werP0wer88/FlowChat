using FlowChat.Application.Abstractions;
namespace FlowChat.SocialGraphService.Application.Contacts.Queries.GetContactsForUser;

public sealed record GetContactsForUserQuery(Guid UserId) : IQuery<IReadOnlyList<ContactDto>>;
