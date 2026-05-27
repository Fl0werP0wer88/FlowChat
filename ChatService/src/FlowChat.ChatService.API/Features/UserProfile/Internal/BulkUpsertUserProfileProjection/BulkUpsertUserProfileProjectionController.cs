using FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertUserProfileProjection;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.ChatService.Api.Features.UserProfile.Internal.BulkUpsertUserProfileProjection;

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
                    item.DisplayName,
                    item.AvatarUrl,
                    item.Source)).ToArray()),
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
