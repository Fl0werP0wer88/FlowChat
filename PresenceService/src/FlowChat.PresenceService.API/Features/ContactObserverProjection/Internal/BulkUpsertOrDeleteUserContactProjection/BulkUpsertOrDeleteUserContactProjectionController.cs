using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertOrDeleteUserContactProjection;
using FlowChat.PresenceService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.PresenceService.API.Features.ContactObserverProjection.Internal.BulkUpsertOrDeleteUserContactProjection;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/presence/contact-observers/projection/bulk-upsert-or-delete")]
public sealed class BulkUpsertOrDeleteUserContactProjectionController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public BulkUpsertOrDeleteUserContactProjectionController(
        IMediator mediator,
        IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpPost]
    public async Task<IActionResult> BulkUpsertOrDelete(
        [FromBody] BulkUpsertOrDeleteUserContactProjectionRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        if (request.Items.Any(i => i.ObservedUserId == Guid.Empty || i.ObserverUserId == Guid.Empty))
        {
            return BadRequest("All items must have valid ObservedUserId and ObserverUserId values.");
        }

        var items = request.Items
            .Select(item => new UserContactProjectionCommandItem(
                item.ObservedUserId,
                item.ObserverUserId,
                item.Value is null ? null : new ContactObserverProjectionDto
                {
                    ObservedUserId = item.ObservedUserId,
                    ObserverUserId = item.ObserverUserId,
                    SourceVersion = item.SourceVersion,
                    Source = item.Value.Source
                },
                item.SourceVersion,
                item.SourceCreatedAtUtc,
                item.SourceLastModifiedAtUtc,
                item.SourceDeletedAtUtc))
            .ToArray();

        var result = await _mediator.Send(
            new BulkUpsertOrDeleteUserContactProjectionCommand(items),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleError(result.Error);
        }

        return NoContent();
    }
}
