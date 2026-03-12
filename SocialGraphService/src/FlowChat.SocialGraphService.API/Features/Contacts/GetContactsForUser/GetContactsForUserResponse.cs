using FlowChat.SocialGraphService.Application.Contacts.Queries.GetContactsForUser;

namespace FlowChat.SocialGraphService.Api.Features.Contacts.GetContactsForUser;

public sealed record GetContactsForUserResponse(IReadOnlyList<ContactDto> Contacts);
