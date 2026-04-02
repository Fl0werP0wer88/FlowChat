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

    [HttpPost]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<RegisterUserCommand>(request);
        var response = await _mediator.Send(command, cancellationToken);

        return response.IsSuccess
            ? Ok(_mapper.Map<RegisterUserResponse>(response.Value))
            : HandleError(response.Error);
    }
}

