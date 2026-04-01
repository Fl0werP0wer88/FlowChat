using FlowChat.Shared.API;
using FlowChat.NotificationService.Application.Features.Notifications.Queries.GetNotifications;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.NotificationService.Api.Features.Notifications.Public.GetNotifications;

[ApiController]
[Route("api/[controller]")]
public sealed class NotificationsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetNotificationsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetNotificationsQuery(userId), cancellationToken);

        return result.IsSuccess
            ? Ok(new GetNotificationsResponse(result.Value))
            : HandleError(result.Error);
    }
}

