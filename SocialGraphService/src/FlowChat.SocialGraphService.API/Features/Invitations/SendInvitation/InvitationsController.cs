using FlowChat.Domain.Abstractions;
using FlowChat.SocialGraphService.Application.Invitations.Commands.SendInvitation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.SocialGraphService.Api.Features.Invitations.SendInvitation;

[ApiController]
[Route("api/[controller]")]
public sealed class InvitationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvitationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(InvitationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Send(
        [FromBody] SendInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SendInvitationCommand(request.RequesterId, request.AddresseeId),
            cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return result.Error.ErrorType.Name switch
        {
            "BadRequest" or "Validation" => BadRequest(new { message = result.Error.ErrorMessage, errors = result.Error.Errors }),
            "Conflict" => Conflict(new { message = result.Error.ErrorMessage }),
            "NotFound" => NotFound(new { message = result.Error.ErrorMessage }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.Error.ErrorMessage })
        };
    }

    public sealed record SendInvitationRequest(Guid RequesterId, Guid AddresseeId);
}
