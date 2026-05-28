using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.UserProfile.Internal.BulkUpsertOrDeleteUserProfileProjection;

public sealed class BulkUpsertOrDeleteUserProfileProjectionRequest : IServiceInput
{
    public IReadOnlyCollection<BulkUpsertOrDeleteUserProfileProjectionRequestItem> Items { get; init; } = [];
}

public sealed class BulkUpsertOrDeleteUserProfileProjectionRequestItem : IServiceInput
{
    public Guid UserProfileId { get; init; }
    public BulkUpsertOrDeleteUserProfileProjectionRequestValue? Value { get; init; }
}

public sealed class BulkUpsertOrDeleteUserProfileProjectionRequestValue : IServiceInput
{
    public string FriendlyUserId { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? AvatarUrl { get; init; }
    public string Source { get; init; } = string.Empty;
}
