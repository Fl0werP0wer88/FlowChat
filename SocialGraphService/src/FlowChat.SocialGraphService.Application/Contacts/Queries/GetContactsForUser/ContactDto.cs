namespace FlowChat.SocialGraphService.Application.Contacts.Queries.GetContactsForUser;

public sealed record ContactDto(
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
