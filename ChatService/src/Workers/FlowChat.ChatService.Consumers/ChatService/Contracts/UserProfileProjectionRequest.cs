using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Consumers.ChatService.Contracts;

public sealed class UserProfileProjectionRequest : IConsumerOutput
{
    public Guid UserProfileId { get; set; }
    public string FriendlyUserId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;
    public DateTimeOffset LastModifiedAtUtc { get; set; }
}
