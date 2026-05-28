using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;

public sealed class BulkUpsertOrDeleteUserProfileProjectionCommandHandler(
    IUnitOfWork unitOfWork,
    IBulkRepository<UserProfileProjectionDto> bulkRepository)
    : BulkUpsertOrDeleteCommandHandlerBase<
        BulkUpsertOrDeleteUserProfileProjectionCommand,
        UserProfileProjectionDto>(unitOfWork, bulkRepository)
{
    protected override IReadOnlyCollection<BulkCommandItem<UserProfileProjectionDto>> GetItems(
        BulkUpsertOrDeleteUserProfileProjectionCommand request) =>
        request.Items
            .Select(item => new BulkCommandItem<UserProfileProjectionDto>(
                item.EntityId,
                item.Value is null ? null : NormalizeDto(item.Value)))
            .ToArray();

    private static UserProfileProjectionDto NormalizeDto(UserProfileProjectionDto dto) => new()
    {
        UserProfileId = dto.UserProfileId,
        FriendlyUserId = dto.FriendlyUserId.Trim(),
        DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? null : dto.DisplayName.Trim(),
        AvatarUrl = string.IsNullOrWhiteSpace(dto.AvatarUrl) ? null : dto.AvatarUrl.Trim(),
        Source = dto.Source.Trim()
    };
}
