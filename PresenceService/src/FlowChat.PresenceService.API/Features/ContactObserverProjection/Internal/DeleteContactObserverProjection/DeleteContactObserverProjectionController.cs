using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.DeleteContactObserverProjection;
using FlowChat.PresenceService.Infrastructure.Configuration.Settings;
using FlowChat.Core.Contracts;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.PresenceService.API.Features.ContactObserverProjection.Internal.DeleteContactObserverProjection;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/presence/contact-observers")]
public sealed class DeleteContactObserverProjectionController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public DeleteContactObserverProjectionController(IMediator mediator, ISettingsProvider settingsProvider)
        : base(() => settingsProvider.GetSection<InternalApiSettingsSection>().ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(settingsProvider);
    }

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
