using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Api.Features.Contact.Public.GetContactsForUser;

public sealed record GetContactsForUserResponse(IReadOnlyList<ContactResponse> Contacts) : IServiceOutput;

public sealed record ContactResponse(
    Guid Id,
    Guid OwnerUserId,
    Guid ContactUserId,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Email,
    bool IsBlocked);
