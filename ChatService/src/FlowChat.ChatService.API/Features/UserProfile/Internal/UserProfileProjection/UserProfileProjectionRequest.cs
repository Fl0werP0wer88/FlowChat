using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.UserProfile.Internal.UserProfileProjection;

public sealed class UserProfileProjectionRequest : IServiceInput
{
    public Guid UserProfileId { get; set; }
    public string FriendlyUserId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Source { get; set; } = string.Empty;
}
