using System.Security.Claims;
using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.RegisterRealtimeConnection;
using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.UnregisterRealtimeConnection;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FlowChat.RealtimeService.Api.Realtime;

[Authorize]
public sealed class ChatHub(
    ILogger<ChatHub> logger,
    IMediator mediator) : Hub<IRealtimeClient>
{
    private const string SubjectClaimType = "sub";
    private readonly ILogger<ChatHub> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    public override async Task OnConnectedAsync()
    {
        var userId = ResolveUserId();
        if (userId is null)
        {
            _logger.LogWarning("Rejecting realtime connection {ConnectionId} because authenticated user id is missing.", Context.ConnectionId);
            Context.Abort();
            return;
        }

        try
        {
            var result = await _mediator.Send(
                new RegisterRealtimeConnectionCommand(userId.Value, Context.ConnectionId),
                Context.ConnectionAborted);
            if (result.IsFailure)
            {
                Context.Abort();
                return;
            }

            await base.OnConnectedAsync();
        }
        catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
        {
            Context.Abort();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to register realtime connection {ConnectionId} for user {UserId}.", Context.ConnectionId, userId.Value);
            Context.Abort();
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            await _mediator.Send(
                new UnregisterRealtimeConnectionCommand(Context.ConnectionId),
                CancellationToken.None);
        }
        finally
        {
            await base.OnDisconnectedAsync(exception);
        }
    }

    private Guid? ResolveUserId()
    {
        var value = Context.User?.FindFirstValue(SubjectClaimType)
            ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
