using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

public sealed record UserProfileDto : IDbReadResponse
{
    public UserProfileDto()
    {
    }

    public UserProfileDto(
        Guid id,
        string friendlyUserId,
        string? avatarUrl,
        string? bio,
        bool isActive,
        DateTimeOffset? lastSeenAtUtc,
        IReadOnlyList<EmailDto> emails,
        IReadOnlyList<PhoneDto> phones)
    {
        Id = id;
        FriendlyUserId = friendlyUserId;
        AvatarUrl = avatarUrl;
        Bio = bio;
        IsActive = isActive;
        LastSeenAtUtc = lastSeenAtUtc;
        Emails = emails;
        Phones = phones;
    }

    public Guid Id { get; init; }
    public string FriendlyUserId { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Organization { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? LastSeenAtUtc { get; init; }
    public IReadOnlyList<EmailDto> Emails { get; init; } = [];
    public IReadOnlyList<PhoneDto> Phones { get; init; } = [];
}
