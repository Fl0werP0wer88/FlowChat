using AutoMapper;
using FlowChat.API.Abstractions;
using FlowChat.AuthService.Application.Users.Commands.ConfirmUserEmail;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.AuthService.API.Features.Users.ConfirmUserEmail;

[ApiController]
[Route("api/users")]
public sealed class ConfirmUserEmailController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public ConfirmUserEmailController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    [HttpGet("confirm-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] ConfirmUserEmailRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<ConfirmUserEmailCommand>(request);
        var response = await _mediator.Send(command, cancellationToken);

        return response.IsSuccess
            ? NoContent()
            : HandleError(response.Error);
    }
}
