using AutoMapper;
using FlowChat.Shared.API;
using FlowChat.AuthService.Application.Features.Users.Commands.RefreshToken;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.AuthService.API.Features.Users.Public.RefreshToken;

[ApiController]
[Route("api/users")]
public sealed class RefreshTokenController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public RefreshTokenController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var command = _mapper.Map<RefreshTokenCommand>(request);
        var response = await _mediator.Send(command, cancellationToken);

        return response.IsSuccess
            ? Ok(_mapper.Map<RefreshTokenResponse>(response.Value))
            : HandleError(response.Error);
    }
}
