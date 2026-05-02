using FlowChat.Core.Contracts;
using FlowChat.PresenceService.Application.Features.Presence.Queries.GetPresenceStatusesBatch;
using FlowChat.PresenceService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.PresenceService.API.Features.Presence.Internal.GetPresenceStatusesBatch;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/presence/statuses")]
public sealed class GetPresenceStatusesBatchController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public GetPresenceStatusesBatchController(IMediator mediator, ISettingsProvider settingsProvider)
        : base(() => settingsProvider.GetSection<InternalApiSettingsSection>().ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(settingsProvider);
    }

    [HttpPost("batch")]
    public async Task<IActionResult> GetStatuses(
        [FromBody] GetPresenceStatusesBatchRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new GetPresenceStatusesBatchQuery(request.UserIds),
            cancellationToken);

        if (result.IsFailure)
        {
            return HandleError(result.Error);
        }

        return Ok(result.Value.Select(dto => new ContactPresenceStatusResponse
        {
            UserId = dto.UserId,
            Status = dto.Status,
            ChangedAtUtc = dto.ChangedAtUtc
        }));
    }
}
