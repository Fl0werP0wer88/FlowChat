using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.BulkUpsertOrDeleteUserProfileProjection;

public sealed class BulkUpsertOrDeleteUserProfileProjectionRequest : IServiceInput
{
    public IReadOnlyCollection<BulkUpsertOrDeleteUserProfileProjectionRequestItem> Items { get; init; } = [];
}

public sealed class BulkUpsertOrDeleteUserProfileProjectionRequestItem : IServiceInput
{
    public Guid UserProfileId { get; init; }
    public int SourceVersion { get; init; }
    public DateTimeOffset SourceCreatedAtUtc { get; init; }
    public DateTimeOffset SourceLastModifiedAtUtc { get; init; }
    public DateTimeOffset? SourceDeletedAtUtc { get; init; }
    public BulkUpsertOrDeleteUserProfileProjectionRequestValue? Value { get; init; }
}

public sealed class BulkUpsertOrDeleteUserProfileProjectionRequestValue : IServiceInput
{
    public string FriendlyUserId { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Organization { get; init; }
    public string? MainEmailAddress { get; init; }
    public bool? MainEmailIsConfirmed { get; init; }
    public bool? MainEmailIsVisible { get; init; }
    public string? MainPhoneNumber { get; init; }
    public bool? MainPhoneIsConfirmed { get; init; }
    public bool? MainPhoneIsVisible { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? LastSeenAtUtc { get; init; }
    public string Source { get; init; } = string.Empty;
}
