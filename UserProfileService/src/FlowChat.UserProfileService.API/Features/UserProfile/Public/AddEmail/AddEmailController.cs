using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddEmail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.AddEmail;

[ApiController]
[Authorize]
[Route("api/userprofiles")]
public sealed class AddEmailController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AddEmailController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPut("emails")]
    [ProducesResponseType(typeof(AddEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AddEmailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddEmail(
        [FromBody] AddEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new AddEmailCommand(userId, request.EmailId, request.Address), cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleError(result.Error);
        }

        var response = new AddEmailResponse(result.Value.Value);
        return result.Value.WasAlreadyProcessed
            ? Ok(response)
            : StatusCode(StatusCodes.Status201Created, response);
    }
}
