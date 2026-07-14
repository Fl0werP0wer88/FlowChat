using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Queries.GetContactsForUser;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetContactsForUser;

[ApiController]
[Authorize]
[Route("api/conversations")]
public sealed class GetContactsForUserController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public GetContactsForUserController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    [HttpGet("contacts")]
    [ProducesResponseType(typeof(GetContactsForUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetContactsForUser(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetContactsForUserQuery(userId), cancellationToken);

        if (!result.IsSuccess)
            return HandleError(result.Error);

        var response = new GetContactsForUserResponse(
            _mapper.Map<IReadOnlyCollection<ContactResponse>>(result.Value));

        return Ok(response);
    }
}
