using AutoMapper;
using FlowChat.Shared.API;
using FlowChat.AuthService.Application.Features.User.Commands.RegisterUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.AuthService.API.Features.User.Public.RegisterUser;

[ApiController]
[Route("api/users")]
public sealed class RegisterUserController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public RegisterUserController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    [HttpPut]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<RegisterUserCommand>(request);
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.IsSuccess)
        {
            return HandleError(response.Error);
        }

        var body = _mapper.Map<RegisterUserResponse>(response.Value);
        return StatusCode(StatusCodes.Status201Created, body);
    }
}

