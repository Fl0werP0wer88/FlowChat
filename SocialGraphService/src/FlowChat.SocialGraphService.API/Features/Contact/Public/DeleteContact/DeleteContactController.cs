using FlowChat.Shared.API;
using FlowChat.SocialGraphService.Application.Features.Contact.Commands.DeleteContact;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.SocialGraphService.Api.Features.Contact.Public.DeleteContact;

[ApiController]
[Route("api/contacts")]
public sealed class DeleteContactController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public DeleteContactController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpDelete("{ownerUserId:guid}/{contactUserId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid ownerUserId,
        [FromRoute] Guid contactUserId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeleteContactCommand(ownerUserId, contactUserId),
            cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : HandleError(result.Error);
    }
}
