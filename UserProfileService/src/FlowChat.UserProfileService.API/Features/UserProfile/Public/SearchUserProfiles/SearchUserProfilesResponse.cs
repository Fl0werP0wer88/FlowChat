using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.SearchUserProfiles;

public sealed record SearchUserProfilesResponse(IReadOnlyList<UserProfileResponse> UserProfiles) : IServiceOutput;

public sealed record UserProfileResponse(
    Guid Id,
    string FriendlyUserId,
    string? FirstName,
    string? LastName,
    string? Organization,
    string? AvatarUrl,
    string? Bio,
    bool IsActive,
    DateTimeOffset? LastSeenAtUtc,
    IReadOnlyList<EmailResponse> Emails,
    IReadOnlyList<PhoneResponse> Phones);

public sealed record EmailResponse(
    Guid Id,
    string Address,
    bool IsMain,
    bool IsAuth,
    bool IsConfirmed,
    bool IsVisible);

public sealed record PhoneResponse(
    Guid Id,
    string Number,
    bool IsMain,
    bool IsConfirmed,
    bool IsVisible);
