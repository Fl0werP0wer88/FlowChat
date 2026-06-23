using FlowChat.Shared.Application;
namespace FlowChat.SocialGraphService.Application.Features.Contact.Queries.GetContactsForUser;

public sealed record GetContactsForUserQuery(Guid UserId) : IQuery<IReadOnlyList<ContactDto>>;

