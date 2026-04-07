using FlowChat.Shared.API;
using FlowChat.SocialGraphService.Application.Features.Contact.Commands.AddContact;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.SocialGraphService.Api.Features.Contact.Public.AddContact;

[ApiController]
[Route("api/contacts")]
public sealed class AddContactController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AddContactController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AddContactResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Add(
        [FromBody] AddContactRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AddContactCommand(
                request.OwnerUserId,
                request.UserId,
                request.FriendlyUserId,
                request.Email),
            cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, new AddContactResponse(result.Value))
            : HandleError(result.Error);
    }
}
