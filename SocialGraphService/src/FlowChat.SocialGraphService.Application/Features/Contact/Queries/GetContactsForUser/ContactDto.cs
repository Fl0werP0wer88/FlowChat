using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Application.Features.Contact.Queries.GetContactsForUser;

public sealed record ContactDto(
    Guid Id,
    Guid OwnerUserId,
    Guid ContactUserId,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Email,
    bool IsBlocked) : IDbReadResponse;
