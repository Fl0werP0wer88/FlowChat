using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.InsertUserProfileProjection;

public sealed record InsertUserProfileProjectionCommand(
    Guid UserProfileId,
    string? FriendlyUserId,
    string? DisplayName,
    string? MainEmail,
    string? MainPhone,
    string? AvatarUrl,
    string? Bio,
    bool IsActive,
    DateTimeOffset? LastSeenAtUtc,
    bool IsEmailVisible,
    bool IsPhoneVisible) : ICommand<Unit>;
