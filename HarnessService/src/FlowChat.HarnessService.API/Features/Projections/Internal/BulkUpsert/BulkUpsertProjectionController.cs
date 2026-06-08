using FlowChat.HarnessService.API.Configuration.Settings;
using FlowChat.HarnessService.Application.Features.Projections;
using FlowChat.HarnessService.Application.Features.Projections.Commands.BulkUpsert;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.HarnessService.API.Features.Projections.Internal.BulkUpsert;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/projections/bulk-upsert")]
public sealed class BulkUpsertProjectionController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public BulkUpsertProjectionController(
        IMediator mediator,
        IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpPost]
    public async Task<IActionResult> BulkUpsert(
        [FromBody] BulkUpsertProjectionRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        if (request.Items.Any(i => i.Id == Guid.Empty))
        {
            return BadRequest("All items must have a valid Id.");
        }

        var items = request.Items
            .Select(item => new ProjectionCommandItem(
                item.Id,
                item.Payload is null ? null : new ProjectionTestDto
                {
                    Id = item.Id,
                    Payload = item.Payload
                },
                item.SourceVersion,
                item.SourceCreatedAtUtc,
                item.SourceLastModifiedAtUtc,
                item.SourceDeletedAtUtc))
            .ToArray();

        var result = await _mediator.Send(
            new BulkUpsertProjectionCommand(items),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleError(result.Error);
        }

        return NoContent();
    }
}
