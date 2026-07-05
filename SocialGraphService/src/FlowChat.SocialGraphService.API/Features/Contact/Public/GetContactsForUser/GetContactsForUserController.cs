using AutoMapper;
using FlowChat.Shared.API;
using FlowChat.SocialGraphService.Application.Features.Contact.Queries.GetContactsForUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.SocialGraphService.Api.Features.Contact.Public.GetContactsForUser;

[ApiController]
[Authorize]
[Route("api/contacts")]
public sealed class GetContactsForUserController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public GetContactsForUserController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetContactsForUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetForUser(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetContactsForUserQuery(userId), cancellationToken);

        return result.IsSuccess
            ? Ok(new GetContactsForUserResponse(_mapper.Map<IReadOnlyList<ContactResponse>>(result.Value)))
            : HandleError(result.Error);
    }
}
