using FlowChat.PresenceService.Application.Features.Presence.Commands.InitializePresenceStatus;
using FlowChat.PresenceService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.PresenceService.API.Features.Presence.Internal.InitializePresenceStatus;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/presence/status")]
public sealed class InitializePresenceStatusController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public InitializePresenceStatusController(IMediator mediator, IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize(
        [FromBody] InitializePresenceStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new InitializePresenceStatusCommand(request.UserId),
            cancellationToken);

        return result.IsSuccess ? Accepted() : HandleError(result.Error);
    }
}
