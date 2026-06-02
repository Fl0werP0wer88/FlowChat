using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;

public sealed class UserProfileProjectionRequest : IConsumerOutput
{
    public Guid UserProfileId { get; set; }
    public string FriendlyUserId { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Organization { get; set; }
    public string? MainEmailAddress { get; set; }
    public bool? MainEmailIsConfirmed { get; set; }
    public bool? MainEmailIsVisible { get; set; }
    public string? MainPhoneNumber { get; set; }
    public bool? MainPhoneIsConfirmed { get; set; }
    public bool? MainPhoneIsVisible { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? LastSeenAtUtc { get; set; }
    public int SourceVersion { get; set; }
    public string Source { get; set; } = string.Empty;
}
