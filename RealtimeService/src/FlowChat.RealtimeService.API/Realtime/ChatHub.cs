using System.Security.Claims;
using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FlowChat.RealtimeService.Api.Realtime;

[Authorize]
public sealed class ChatHub(
    ILogger<ChatHub> logger,
    IRealtimeConnectionRegistry realtimeConnectionRegistry) : Hub<IRealtimeClient>
{
    private const string SubjectClaimType = "sub";
    private readonly ILogger<ChatHub> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IRealtimeConnectionRegistry _realtimeConnectionRegistry = realtimeConnectionRegistry
        ?? throw new ArgumentNullException(nameof(realtimeConnectionRegistry));

    public override async Task OnConnectedAsync()
    {
        var userId = ResolveUserId();
        if (userId is null)
        {
            _logger.LogWarning("Rejecting realtime connection {ConnectionId} because authenticated user id is missing.", Context.ConnectionId);
            Context.Abort();
            return;
        }

        var addedToGroup = false;
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.ForUser(userId.Value));
            addedToGroup = true;

            await _realtimeConnectionRegistry.RegisterAsync(
                userId.Value,
                Context.ConnectionId,
                UserStatus.Active,
                Context.ConnectionAborted);
            await base.OnConnectedAsync();
        }
        catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
        {
            await CleanupFailedConnectionAsync(userId.Value, addedToGroup);
            Context.Abort();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to register realtime connection {ConnectionId} for user {UserId}.", Context.ConnectionId, userId.Value);
            await CleanupFailedConnectionAsync(userId.Value, addedToGroup);
            Context.Abort();
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            await _realtimeConnectionRegistry.UnregisterAsync(Context.ConnectionId, CancellationToken.None);
        }
        catch (Exception unregisterException)
        {
            _logger.LogError(unregisterException, "Failed to unregister realtime connection {ConnectionId} from Redis.", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private Guid? ResolveUserId()
    {
        var value = Context.User?.FindFirstValue(SubjectClaimType)
            ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private async Task CleanupFailedConnectionAsync(Guid userId, bool addedToGroup)
    {
        try
        {
            await _realtimeConnectionRegistry.UnregisterAsync(Context.ConnectionId, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to clean up realtime connection {ConnectionId} in Redis after connection failure.", Context.ConnectionId);
        }

        if (!addedToGroup)
        {
            return;
        }

        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.ForUser(userId));
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to remove realtime connection {ConnectionId} from user group after connection failure.", Context.ConnectionId);
        }
    }
}
