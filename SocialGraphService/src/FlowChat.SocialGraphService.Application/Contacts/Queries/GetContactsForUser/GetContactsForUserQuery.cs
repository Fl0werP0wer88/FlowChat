using FlowChat.SocialGraphService.Application.Contracts;
using FlowChat.SocialGraphService.Domain.Enums;
namespace FlowChat.SocialGraphService.Application.Contacts.Queries.GetContactsForUser;

public sealed record GetContactsForUserQuery(
    Guid UserId,
    InvitationStatus? Status = null) : IQuery<IReadOnlyList<ContactDto>>;
