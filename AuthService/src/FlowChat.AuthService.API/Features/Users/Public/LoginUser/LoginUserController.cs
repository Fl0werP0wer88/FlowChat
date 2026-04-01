using AutoMapper;
using FlowChat.Shared.API;
using FlowChat.AuthService.Application.Features.Users.Commands.LoginUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.AuthService.API.Features.Users.Public.LoginUser;

[ApiController]
[Route("api/users")]
public sealed class LoginUserController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public LoginUserController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginUserRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<LoginUserCommand>(request);
        var response = await _mediator.Send(command, cancellationToken);

        return response.IsSuccess
            ? Ok(_mapper.Map<LoginUserResponse>(response.Value))
            : HandleError(response.Error);
    }
}

