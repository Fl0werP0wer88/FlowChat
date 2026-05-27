using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertUserProfileProjection;

public sealed class BulkUpsertUserProfileProjectionCommandHandler(
    IUnitOfWork unitOfWork,
    IBulkUpsertExecutor<UserProfileProjectionDto> bulkUpsertExecutor)
    : BulkUpsertCommandHandlerBase<
        BulkUpsertUserProfileProjectionCommand,
        BulkUpsertUserProfileProjectionCommandItem,
        UserProfileProjectionDto>(unitOfWork, bulkUpsertExecutor)
{
    protected override UserProfileProjectionDto MapItem(BulkUpsertUserProfileProjectionCommandItem item)
    {
        return new UserProfileProjectionDto
        {
            UserProfileId = item.UserProfileId,
            FriendlyUserId = item.FriendlyUserId!.Trim(),
            DisplayName = NormalizeOptional(item.DisplayName),
            AvatarUrl = NormalizeOptional(item.AvatarUrl),
            Source = item.Source!.Trim()
        };
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
