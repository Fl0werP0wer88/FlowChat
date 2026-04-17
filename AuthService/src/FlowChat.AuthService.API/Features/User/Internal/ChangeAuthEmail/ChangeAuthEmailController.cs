using FlowChat.AuthService.Application.Features.User.Commands.ChangeAuthEmail;
using FlowChat.AuthService.Infrastructure.Configuration;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.AuthService.Api.Features.User.Internal.ChangeAuthEmail;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/users")]
public sealed class ChangeAuthEmailController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ChangeAuthEmailController(IMediator mediator, IApiSettingsManager apiSettingsManager)
        : base(() => apiSettingsManager.GetInternalApiSettingsSection().ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(apiSettingsManager);
    }

    [HttpPut("auth-email")]
    public async Task<IActionResult> ChangeAuthEmail(
        [FromBody] ChangeAuthEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new ChangeAuthEmailCommand
            {
                UserId = request.UserId,
                EmailAddress = request.EmailAddress
            },
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }
}
