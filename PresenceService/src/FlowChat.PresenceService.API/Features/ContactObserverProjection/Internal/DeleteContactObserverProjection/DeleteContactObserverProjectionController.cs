using FlowChat.PresenceService.API.Features.ContactObserverProjection.Internal;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.DeleteContactObserverProjection;
using FlowChat.PresenceService.Infrastructure.Configuration;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.PresenceService.API.Features.ContactObserverProjection.Internal.DeleteContactObserverProjection;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/presence/contact-observers")]
public sealed class DeleteContactObserverProjectionController(
    IMediator mediator,
    IApiSettingsManager apiSettingsManager)
    : InternalContactObserverProjectionControllerBase(apiSettingsManager)
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete(
        [FromBody] ContactObserverProjectionRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new DeleteContactObserverProjectionCommand(request.ObservedUserId, request.ObserverUserId),
            cancellationToken);

        return result.IsSuccess ? Accepted() : HandleError(result.Error);
    }
}
