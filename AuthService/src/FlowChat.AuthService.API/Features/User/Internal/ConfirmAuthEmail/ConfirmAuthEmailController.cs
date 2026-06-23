using FlowChat.AuthService.Application.Features.User.Commands.ConfirmAuthEmail;
using FlowChat.AuthService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.AuthService.Api.Features.User.Internal.ConfirmAuthEmail;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/users")]
public sealed class ConfirmAuthEmailController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ConfirmAuthEmailController(IMediator mediator, IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
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
