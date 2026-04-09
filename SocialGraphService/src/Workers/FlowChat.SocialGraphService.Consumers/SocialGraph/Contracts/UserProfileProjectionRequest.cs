using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;

public sealed class UserProfileProjectionRequest : IConsumerOutput
{
    public Guid UserProfileId { get; set; }
    public string FriendlyUserId { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Organization { get; set; }
    public string? MainEmail { get; set; }
    public string? MainPhone { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? LastSeenAtUtc { get; set; }
}
