using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversationsForContacts;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.GetDuetConversationsForContacts;

[ApiController]
[Authorize]
[Route("api/conversations/duet")]
public sealed class GetDuetConversationsForContactsController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public GetDuetConversationsForContactsController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    [HttpPost("for-contacts")]
    [ProducesResponseType(typeof(GetDuetConversationsForContactsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDuetConversationsForContacts(
        [FromBody] GetDuetConversationsForContactsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new GetDuetConversationsForContactsQuery(userId, request.PartnerUserIds),
            cancellationToken);

        if (!result.IsSuccess)
            return HandleError(result.Error);

        var response = new GetDuetConversationsForContactsResponse(
            _mapper.Map<IReadOnlyCollection<DuetConversationForContactResponse>>(result.Value));

        return Ok(response);
    }
}
