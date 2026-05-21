using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

public sealed record UserProfileDto : IDbReadResponse
{
    private Guid _id;

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
        _id = id;
        FriendlyUserId = friendlyUserId;
        AvatarUrl = avatarUrl;
        Bio = bio;
        IsActive = isActive;
        LastSeenAtUtc = lastSeenAtUtc;
        Emails = emails;
        Phones = phones;
    }

    public Guid Id
    {
        get => _id;
        init => _id = value;
    }

    public Guid UserProfileId
    {
        get => _id;
        init => _id = value;
    }

    public string FriendlyUserId { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Organization { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? LastSeenAtUtc { get; init; }
    public EmailDto? MainEmail { get; init; }
    public PhoneDto? MainPhone { get; init; }
    public IReadOnlyList<EmailDto> Emails { get; init; } = [];
    public IReadOnlyList<PhoneDto> Phones { get; init; } = [];
}
