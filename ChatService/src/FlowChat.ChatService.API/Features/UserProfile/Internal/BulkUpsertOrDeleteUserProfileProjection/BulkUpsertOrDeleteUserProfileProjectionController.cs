using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.ChatService.Api.Features.UserProfile.Internal.BulkUpsertOrDeleteUserProfileProjection;

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
            .Select(item => new BulkCommandItem<UserProfileProjectionDto>(
                Id<UserProfileProjectionDto>.FromGuid(item.UserProfileId),
                item.Value is null ? null : new UserProfileProjectionDto
                {
                    UserProfileId = item.UserProfileId,
                    FriendlyUserId = item.Value.FriendlyUserId,
                    DisplayName = item.Value.DisplayName,
                    AvatarUrl = item.Value.AvatarUrl,
                    Source = item.Value.Source
                }))
            .ToArray();

        var result = await _mediator.Send(
            new BulkUpsertOrDeleteUserProfileProjectionCommand(items),
            cancellationToken);

        if (!result.IsSuccess)
            return HandleError(result.Error);

        return Ok(new BulkUpsertOrDeleteUserProfileProjectionResponse(
            result.Value.RequestedCount,
            result.Value.UpsertedCount,
            result.Value.DeletedCount));
    }
}
