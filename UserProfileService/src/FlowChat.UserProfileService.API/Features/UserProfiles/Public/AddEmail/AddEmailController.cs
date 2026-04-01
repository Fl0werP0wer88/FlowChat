using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.AddEmail;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfiles.Public.AddEmail;

[ApiController]
[Route("api/userprofiles")]
public sealed class AddEmailController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AddEmailController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{userId:guid}/emails")]
    [ProducesResponseType(typeof(AddEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddEmail(
        [FromRoute] Guid userId,
        [FromBody] AddEmailRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AddEmailCommand(userId, request.Address), cancellationToken);

        return result.IsSuccess
            ? Ok(new AddEmailResponse(result.Value))
            : HandleError(result.Error);
    }
}

