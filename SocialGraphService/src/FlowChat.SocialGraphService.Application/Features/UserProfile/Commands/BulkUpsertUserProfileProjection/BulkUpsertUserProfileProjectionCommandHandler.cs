using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Application.Features.UserProfile;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.BulkUpsertUserProfileProjection;

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
            FirstName = NormalizeOptional(item.FirstName),
            LastName = NormalizeOptional(item.LastName),
            Organization = NormalizeOptional(item.Organization),
            MainEmail = CreateMainEmail(
                item.MainEmailAddress,
                item.MainEmailIsConfirmed,
                item.MainEmailIsVisible),
            MainPhone = CreateMainPhone(
                item.MainPhoneNumber,
                item.MainPhoneIsConfirmed,
                item.MainPhoneIsVisible),
            AvatarUrl = NormalizeOptional(item.AvatarUrl),
            Bio = NormalizeOptional(item.Bio),
            IsActive = item.IsActive,
            LastSeenAtUtc = item.LastSeenAtUtc
        };
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static UserProfileProjectionEmailDto? CreateMainEmail(
        string? address,
        bool? isConfirmed,
        bool? isVisible)
    {
        var normalizedAddress = NormalizeOptional(address);
        return normalizedAddress == null
            ? null
            : new UserProfileProjectionEmailDto
            {
                Address = normalizedAddress,
                IsConfirmed = isConfirmed ?? false,
                IsVisible = isVisible ?? false
            };
    }

    private static UserProfileProjectionPhoneDto? CreateMainPhone(
        string? number,
        bool? isConfirmed,
        bool? isVisible)
    {
        var normalizedNumber = NormalizeOptional(number);
        return normalizedNumber == null
            ? null
            : new UserProfileProjectionPhoneDto
            {
                Number = normalizedNumber,
                IsConfirmed = isConfirmed ?? false,
                IsVisible = isVisible ?? false
            };
    }
}
