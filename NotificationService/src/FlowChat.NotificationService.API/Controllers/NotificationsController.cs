using FlowChat.NotificationService.Application.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.NotificationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        var notifications = await _mediator.Send(new GetNotificationsQuery(userId), cancellationToken);
        return Ok(notifications);
    }
}
