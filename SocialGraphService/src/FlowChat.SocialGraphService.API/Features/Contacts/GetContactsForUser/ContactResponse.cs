namespace FlowChat.SocialGraphService.Api.Features.Contacts.GetContactsForUser;

public sealed record ContactResponse(
    Guid Id,
    Guid OwnerUserId,
    Guid ContactUserId,
    string Login,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Email,
    bool IsBlocked,
    DateTime CreatedDate,
    DateTime LastModifiedDate);
