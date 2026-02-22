using FlowChat.SocialGraphService.Application.Invitations;
using FlowChat.SocialGraphService.Application.Invitations.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.SocialGraphService.Api.Controllers;

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
    [ProducesResponseType(typeof(InvitationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Send(
        [FromBody] SendInvitationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var invitation = await _mediator.Send(
                new SendInvitationCommand(request.RequesterId, request.AddresseeId),
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, invitation);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    public sealed record SendInvitationRequest(Guid RequesterId, Guid AddresseeId);
}
