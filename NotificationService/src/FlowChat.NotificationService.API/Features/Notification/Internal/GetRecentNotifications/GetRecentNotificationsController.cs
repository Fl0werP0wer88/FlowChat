using FlowChat.Core.Contracts;
using FlowChat.NotificationService.Application.Features.Notification.Queries.GetNotifications;
using FlowChat.NotificationService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.NotificationService.Api.Features.Notification.Internal.GetRecentNotifications;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/notifications")]
public sealed class GetRecentNotificationsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public GetRecentNotificationsController(IMediator mediator, IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpGet("recent")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetNotificationsQuery(null), cancellationToken);

        return result.IsSuccess
            ? Ok(new GetRecentNotificationsResponse(result.Value))
            : HandleError(result.Error);
    }
}

public sealed record GetRecentNotificationsResponse(IReadOnlyList<NotificationDto> Notifications) : IServiceOutput;
