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

    public GetContactsForUserController(IMediator mediator)
    {
        _mediator = mediator;
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
            ? Ok(new GetContactsForUserResponse([.. result.Value.Select(MapToResponse)]))
            : HandleError(result.Error);
    }

    private static ContactResponse MapToResponse(ContactDto contact) =>
        new(
            contact.Id,
            contact.OwnerUserId,
            contact.ContactUserId,
            contact.DisplayName,
            contact.FirstName,
            contact.LastName,
            contact.PhoneNumber,
            contact.Email,
            contact.IsBlocked);
}
