using FlowChat.SocialGraphService.Application.Features.Contacts.Queries.GetContactsForUser;

namespace FlowChat.SocialGraphService.Api.Features.Contacts.Public.GetContactsForUser;

public sealed record GetContactsForUserResponse(IReadOnlyList<ContactDto> Contacts);
