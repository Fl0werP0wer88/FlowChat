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
            .Select(item => new UserProfileProjectionCommandItem(
                Id<UserProfileProjectionDto>.FromGuid(item.UserProfileId),
                item.Value is null ? null : new UserProfileProjectionDto
                {
                    UserProfileId = item.UserProfileId,
                    FriendlyUserId = item.Value.FriendlyUserId,
                    FirstName = item.Value.FirstName,
                    LastName = item.Value.LastName,
                    AvatarUrl = item.Value.AvatarUrl,
                    SourceVersion = item.SourceVersion,
                    Source = item.Value.Source
                },
                item.SourceVersion,
                item.SourceCreatedAtUtc,
                item.SourceLastModifiedAtUtc,
                item.SourceDeletedAtUtc))
            .ToArray();

        var result = await _mediator.Send(
            new BulkUpsertOrDeleteUserProfileProjectionCommand(items),
            cancellationToken);

        if (!result.IsSuccess)
            return HandleError(result.Error);

        return NoContent();
    }
}
