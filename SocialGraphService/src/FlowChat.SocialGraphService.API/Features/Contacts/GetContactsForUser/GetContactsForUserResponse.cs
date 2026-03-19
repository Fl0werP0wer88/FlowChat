namespace FlowChat.SocialGraphService.Api.Features.Contacts.GetContactsForUser;

public sealed record GetContactsForUserResponse(IReadOnlyList<ContactResponse> Contacts);
