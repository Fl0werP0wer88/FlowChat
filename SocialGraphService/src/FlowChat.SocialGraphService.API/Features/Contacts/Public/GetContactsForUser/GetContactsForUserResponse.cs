namespace FlowChat.SocialGraphService.Api.Features.Contacts.Public.GetContactsForUser;

public sealed record GetContactsForUserResponse(IReadOnlyList<ContactResponse> Contacts);
