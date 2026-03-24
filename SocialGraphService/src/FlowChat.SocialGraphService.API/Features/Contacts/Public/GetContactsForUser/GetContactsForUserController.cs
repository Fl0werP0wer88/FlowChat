using AutoMapper;
using FlowChat.API.Abstractions;
using FlowChat.SocialGraphService.Application.Contacts.Queries.GetContactsForUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.SocialGraphService.Api.Features.Contacts.Public.GetContactsForUser;

[ApiController]
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

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(GetContactsForUserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForUser(
        [FromRoute] GetContactsForUserRequest request,
        CancellationToken cancellationToken)
    {
        var query = _mapper.Map<GetContactsForUserQuery>(request);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(new GetContactsForUserResponse(result.Value))
            : HandleError(result.Error);
    }
}
