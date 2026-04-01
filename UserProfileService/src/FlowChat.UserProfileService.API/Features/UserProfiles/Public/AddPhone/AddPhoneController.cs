using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.AddPhone;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfiles.Public.AddPhone;

[ApiController]
[Route("api/userprofiles")]
public sealed class AddPhoneController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AddPhoneController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{userId:guid}/phones")]
    [ProducesResponseType(typeof(AddPhoneResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddPhone(
        [FromRoute] Guid userId,
        [FromBody] AddPhoneRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AddPhoneCommand(userId, request.Number), cancellationToken);

        return result.IsSuccess
            ? Ok(new AddPhoneResponse(result.Value))
            : HandleError(result.Error);
    }
}

