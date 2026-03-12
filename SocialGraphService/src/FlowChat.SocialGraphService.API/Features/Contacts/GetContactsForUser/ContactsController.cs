using FlowChat.API.Abstractions;
using FlowChat.SocialGraphService.Application.Contacts.Queries.GetContactsForUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.SocialGraphService.Api.Features.Contacts.GetContactsForUser;

[ApiController]
[Route("api/[controller]")]
public sealed class ContactsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ContactsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<ContactDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForUser(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var contacts = await _mediator.Send(
            new GetContactsForUserQuery(userId),
            cancellationToken);

        return Ok(contacts);
    }
}
