namespace FlowChat.SocialGraphService.Application.Contacts.Queries;

public sealed record ContactDto(
    Guid Id,
    Guid UserId1,
    Guid UserId2,
    bool IsBlocked,
    Guid? BlockedBy,
    DateTime CreatedDate,
    DateTime LastModifiedDate);
