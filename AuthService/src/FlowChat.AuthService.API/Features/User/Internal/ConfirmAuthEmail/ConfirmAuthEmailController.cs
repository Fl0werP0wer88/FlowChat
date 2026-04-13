using FlowChat.AuthService.Application.Features.User.Commands.ConfirmAuthEmail;
using FlowChat.AuthService.Infrastructure.Configuration;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.AuthService.Api.Features.User.Internal.ConfirmAuthEmail;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/users")]
public sealed class ConfirmAuthEmailController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ConfirmAuthEmailController(IMediator mediator, IApiSettingsManager apiSettingsManager)
        : base(() => apiSettingsManager.GetInternalApiSettings().ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(apiSettingsManager);
    }

    [HttpPost("email-confirmation")]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmAuthEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new ConfirmAuthEmailCommand
            {
                EmailAddress = request.EmailAddress
            },
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }
}
