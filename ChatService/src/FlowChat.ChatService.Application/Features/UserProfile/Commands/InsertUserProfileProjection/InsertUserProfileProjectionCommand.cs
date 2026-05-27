using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.UserProfile.Commands.InsertUserProfileProjection;

public sealed record InsertUserProfileProjectionCommand(
    Guid UserProfileId,
    string? FriendlyUserId,
    string? DisplayName,
    string? AvatarUrl,
    string? Source) : ICommand<IdempotentCommandResult<Unit>>
{
    public const string IdempotencyConflictKey = nameof(InsertUserProfileProjectionCommand);
}
