using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertContactObserverProjection;
using FlowChat.PresenceService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.PresenceService.API.Features.ContactObserverProjection.Internal.BulkUpsertContactObserverProjection;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/presence/contact-observers/bulk-upsert")]
public sealed class BulkUpsertContactObserverProjectionController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public BulkUpsertContactObserverProjectionController(
        IMediator mediator,
        IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpPost]
    public async Task<IActionResult> BulkUpsert(
        [FromBody] BulkUpsertContactObserverProjectionRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new BulkUpsertContactObserverProjectionCommand(
                (request.Items ?? []).Select(item => new BulkUpsertContactObserverProjectionCommandItem(
                    item.ObservedUserId,
                    item.ObserverUserId)).ToArray()),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleError(result.Error);
        }

        return Ok(new BulkUpsertContactObserverProjectionResponse(
            result.Value.RequestedCount,
            result.Value.UpsertedCount));
    }
}
