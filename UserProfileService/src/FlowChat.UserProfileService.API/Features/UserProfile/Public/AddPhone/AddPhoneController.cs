using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddPhone;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.AddPhone;

[ApiController]
[Authorize]
[Route("api/userprofiles")]
public sealed class AddPhoneController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AddPhoneController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("phones")]
    [ProducesResponseType(typeof(AddPhoneResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddPhone(
        [FromBody] AddPhoneRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new AddPhoneCommand(userId, request.PhoneId, request.Number), cancellationToken);

        return result.IsSuccess
            ? Ok(new AddPhoneResponse(result.Value))
            : HandleError(result.Error);
    }
}
