namespace FlowChat.SocialGraphService.Application.Features.Contact.Queries.GetContactsForUser;

public sealed record ContactDto(
    Guid Id,
    Guid OwnerUserId,
    Guid ContactUserId,
    string DisplayedName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Email,
    bool IsBlocked);
