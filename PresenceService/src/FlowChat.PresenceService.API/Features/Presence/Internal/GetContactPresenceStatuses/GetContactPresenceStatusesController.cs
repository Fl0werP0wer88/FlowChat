using FlowChat.PresenceService.Application.Features.Presence.Queries.GetContactPresenceStatuses;
using FlowChat.PresenceService.Infrastructure.Configuration;
using FlowChat.Core.Contracts;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.PresenceService.API.Features.Presence.Internal.GetContactPresenceStatuses;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/presence/contacts")]
public sealed class GetContactPresenceStatusesController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public GetContactPresenceStatusesController(IMediator mediator, ISettingsProvider settingsProvider)
        : base(() => settingsProvider.GetSection<InternalApiSettingsSection>().ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(settingsProvider);
    }

    [HttpGet("{userId:guid}/statuses")]
    public async Task<IActionResult> GetStatuses(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new GetContactPresenceStatusesQuery(userId),
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
