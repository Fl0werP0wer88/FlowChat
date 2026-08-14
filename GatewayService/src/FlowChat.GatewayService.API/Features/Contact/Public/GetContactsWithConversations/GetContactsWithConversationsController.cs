using AutoMapper;
using FlowChat.GatewayService.Api.Features.Contact.Interfaces;
using FlowChat.Shared.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.GatewayService.Api.Features.Contact.Public.GetContactsWithConversations;

[ApiController]
[Authorize]
[Route("api/aggregate/contacts")]
public sealed class GetContactsWithConversationsController(
    IContactsFacade contactsFacade,
    IMapper mapper) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(GetContactsWithConversationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetContactsWithConversations(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out _))
        {
            return Unauthorized();
        }

        var result = await contactsFacade.GetContactsWithConversationsAsync(cancellationToken);
        return result.IsSuccess
            ? Ok(mapper.Map<GetContactsWithConversationsResponse>(result.Value))
            : HandleError(result.Error);
    }
}
