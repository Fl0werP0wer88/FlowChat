using FlowChat.Shared.API;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using FlowChat.SocialGraphService.Infrastructure.Configuration.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.BulkUpsertOrDeleteUserProfileProjection;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/userprofiles/projection/bulk-upsert-or-delete")]
public sealed class BulkUpsertOrDeleteUserProfileProjectionController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public BulkUpsertOrDeleteUserProfileProjectionController(
        IMediator mediator,
        IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpPost]
    public async Task<IActionResult> BulkUpsertOrDelete(
        [FromBody] BulkUpsertOrDeleteUserProfileProjectionRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
            return Unauthorized();

        if (request.Items.Any(i => i.UserProfileId == Guid.Empty))
            return BadRequest("All items must have a valid UserProfileId.");

        var items = request.Items
            .Select(item => new UserProfileProjectionCommandItem(
                Id<UserProfileProjectionDto>.FromGuid(item.UserProfileId),
                item.Value is null ? null : new UserProfileProjectionDto
                {
                    UserProfileId = item.UserProfileId,
                    FriendlyUserId = item.Value.FriendlyUserId,
                    FirstName = item.Value.FirstName,
                    LastName = item.Value.LastName,
                    Organization = item.Value.Organization,
                    MainEmail = CreateMainEmail(item.Value),
                    MainPhone = CreateMainPhone(item.Value),
                    AvatarUrl = item.Value.AvatarUrl,
                    Bio = item.Value.Bio,
                    IsActive = item.Value.IsActive,
                    LastSeenAtUtc = item.Value.LastSeenAtUtc,
                    SourceVersion = item.SourceVersion,
                    Source = item.Value.Source
                },
                item.SourceVersion))
            .ToArray();

        var result = await _mediator.Send(
            new BulkUpsertOrDeleteUserProfileProjectionCommand(items),
            cancellationToken);

        if (!result.IsSuccess)
            return HandleError(result.Error);

        return NoContent();
    }

    private static UserProfileProjectionEmailDto? CreateMainEmail(
        BulkUpsertOrDeleteUserProfileProjectionRequestValue value) =>
        value.MainEmailAddress == null
            ? null
            : new UserProfileProjectionEmailDto
            {
                Address = value.MainEmailAddress,
                IsConfirmed = value.MainEmailIsConfirmed ?? false,
                IsVisible = value.MainEmailIsVisible ?? false
            };

    private static UserProfileProjectionPhoneDto? CreateMainPhone(
        BulkUpsertOrDeleteUserProfileProjectionRequestValue value) =>
        value.MainPhoneNumber == null
            ? null
            : new UserProfileProjectionPhoneDto
            {
                Number = value.MainPhoneNumber,
                IsConfirmed = value.MainPhoneIsConfirmed ?? false,
                IsVisible = value.MainPhoneIsVisible ?? false
            };
}
