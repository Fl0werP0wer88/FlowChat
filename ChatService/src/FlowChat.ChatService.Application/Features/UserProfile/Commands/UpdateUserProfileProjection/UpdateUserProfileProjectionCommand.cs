using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.UserProfile.Commands.UpdateUserProfileProjection;

public sealed record UpdateUserProfileProjectionCommand(
    Guid UserProfileId,
    string? FriendlyUserId,
    string? DisplayName,
    string? AvatarUrl,
    string? CreatedBy,
    DateTimeOffset CreatedAtUtc,
    string? LastModifiedBy,
    DateTimeOffset LastModifiedAtUtc) : ICommand<Unit>;
