using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.UserProfileService.Domain.Entities.UserProfile;

public sealed record UserProfileState
{
    public Guid Id { get; init; }
    public required string FriendlyUserId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Organization { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public bool IsActive { get; init; }
    public UtcDateTimeOffset? LastSeenAtUtc { get; init; }
    public required IReadOnlyList<UserProfileEmailState> Emails { get; init; }
    public required IReadOnlyList<UserProfilePhoneState> Phones { get; init; }
}

public sealed record UserProfileEmailState
{
    public Guid Id { get; init; }
    public Guid UserProfileId { get; init; }
    public required string Address { get; init; }
    public bool IsMain { get; init; }
    public bool IsAuth { get; init; }
    public bool IsConfirmed { get; init; }
    public bool IsVisible { get; init; }
}

public sealed record UserProfilePhoneState
{
    public Guid Id { get; init; }
    public Guid UserProfileId { get; init; }
    public required string Number { get; init; }
    public bool IsMain { get; init; }
    public bool IsConfirmed { get; init; }
    public bool IsVisible { get; init; }
}
