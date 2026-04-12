using FlowChat.PresenceService.API.Features.ContactObserverProjection.Internal;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.InsertContactObserverProjection;
using FlowChat.PresenceService.Infrastructure.Configuration;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.PresenceService.API.Features.ContactObserverProjection.Internal.InsertContactObserverProjection;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/presence/contact-observers")]
public sealed class InsertContactObserverProjectionController(
    IMediator mediator,
    IApiSettingsManager apiSettingsManager)
    : InternalContactObserverProjectionControllerBase(apiSettingsManager)
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    [HttpPost("insert")]
    public async Task<IActionResult> Insert(
        [FromBody] ContactObserverProjectionRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new InsertContactObserverProjectionCommand(request.ObservedUserId, request.ObserverUserId),
            cancellationToken);

        return result.IsSuccess ? Accepted() : HandleError(result.Error);
    }
}
