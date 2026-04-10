using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.UpdateUserProfileProjection;

public sealed record UpdateUserProfileProjectionCommand(
    Guid UserProfileId,
    string? FriendlyUserId,
    string? FirstName,
    string? LastName,
    string? Organization,
    string? MainEmailAddress,
    bool? MainEmailIsConfirmed,
    bool? MainEmailIsVisible,
    string? MainPhoneNumber,
    bool? MainPhoneIsConfirmed,
    bool? MainPhoneIsVisible,
    string? AvatarUrl,
    string? Bio,
    bool IsActive,
    DateTimeOffset? LastSeenAtUtc) : ICommand<Unit>;
