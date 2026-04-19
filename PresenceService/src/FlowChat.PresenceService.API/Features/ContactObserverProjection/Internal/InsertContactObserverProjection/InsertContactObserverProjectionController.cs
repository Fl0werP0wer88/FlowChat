using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.InsertContactObserverProjection;
using FlowChat.PresenceService.Infrastructure.Configuration.Settings;
using FlowChat.Core.Contracts;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.PresenceService.API.Features.ContactObserverProjection.Internal.InsertContactObserverProjection;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/presence/contact-observers")]
public sealed class InsertContactObserverProjectionController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public InsertContactObserverProjectionController(IMediator mediator, ISettingsProvider settingsProvider)
        : base(() => settingsProvider.GetSection<InternalApiSettingsSection>().ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(settingsProvider);
    }

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
