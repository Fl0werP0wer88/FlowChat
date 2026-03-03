using FlowChat.Application.Abstractions;
using FlowChat.SocialGraphService.Domain.Enums;
namespace FlowChat.SocialGraphService.Application.Contacts.Queries.GetContactsForUser;

public sealed record GetContactsForUserQuery(
    Guid UserId,
    InvitationStatus? Status = null) : IQuery<IReadOnlyList<ContactDto>>;
