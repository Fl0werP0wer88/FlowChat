using FlowChat.Shared.API;
using FlowChat.SocialGraphService.Application.Features.Contact.Commands.AddContact;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.SocialGraphService.Api.Features.Contact.Public.AddContact;

[ApiController]
[Authorize]
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Add(
        [FromBody] AddContactRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var ownerUserId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new AddContactCommand(
                request.Id,
                ownerUserId,
                request.UserId,
                request.FriendlyUserId,
                request.Email),
            cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, new AddContactResponse(result.Value))
            : HandleError(result.Error);
    }
}
