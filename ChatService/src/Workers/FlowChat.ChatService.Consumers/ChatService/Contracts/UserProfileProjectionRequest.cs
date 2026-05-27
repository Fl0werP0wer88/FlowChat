using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Consumers.ChatService.Contracts;

public sealed class UserProfileProjectionRequest : IConsumerOutput
{
    public Guid UserProfileId { get; set; }
    public string FriendlyUserId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Source { get; set; } = string.Empty;
}
