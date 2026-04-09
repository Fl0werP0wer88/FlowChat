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
    string? FirstName = null,
    string? LastName = null,
    string? Organization = null) : ICommand<Unit>;
