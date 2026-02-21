using FlowChat.SocialGraphService.Domain.Enums;
using MediatR;

namespace FlowChat.SocialGraphService.Application.Contacts.Queries;

public sealed record GetContactsForUserQuery(
    Guid UserId,
    InvitationStatus? Status = null) : IRequest<IReadOnlyList<ContactDto>>;
