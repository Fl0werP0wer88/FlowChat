using FlowChat.Shared.API;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.BulkUpsertUserProfileProjection;
using FlowChat.SocialGraphService.Infrastructure.Configuration.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.BulkUpsertUserProfileProjection;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/userprofiles/projection/bulk-upsert")]
public sealed class BulkUpsertUserProfileProjectionController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public BulkUpsertUserProfileProjectionController(
        IMediator mediator,
        IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpPost]
    public async Task<IActionResult> BulkUpsert(
        [FromBody] BulkUpsertUserProfileProjectionRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new BulkUpsertUserProfileProjectionCommand(
                (request.Items ?? []).Select(item => new BulkUpsertUserProfileProjectionCommandItem(
                    item.UserProfileId,
                    item.FriendlyUserId,
                    item.FirstName,
                    item.LastName,
                    item.Organization,
                    item.MainEmailAddress,
                    item.MainEmailIsConfirmed,
                    item.MainEmailIsVisible,
                    item.MainPhoneNumber,
                    item.MainPhoneIsConfirmed,
                    item.MainPhoneIsVisible,
                    item.AvatarUrl,
                    item.Bio,
                    item.IsActive,
                    item.LastSeenAtUtc)).ToArray()),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleError(result.Error);
        }

        return Ok(new BulkUpsertUserProfileProjectionResponse(
            result.Value.RequestedCount,
            result.Value.UpsertedCount));
    }
}
