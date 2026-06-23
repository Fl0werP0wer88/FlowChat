using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile;

public sealed class UserProfileProjectionPhoneDto : IDbReadResponse
{
    public required string Number { get; init; }
    public bool IsConfirmed { get; init; }
    public bool IsVisible { get; init; }
}
