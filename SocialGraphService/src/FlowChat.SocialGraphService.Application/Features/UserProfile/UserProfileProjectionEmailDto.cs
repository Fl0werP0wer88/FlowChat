using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile;

public sealed class UserProfileProjectionEmailDto : IDbResponse
{
    public required string Address { get; init; }
    public bool IsConfirmed { get; init; }
    public bool IsVisible { get; init; }
}
