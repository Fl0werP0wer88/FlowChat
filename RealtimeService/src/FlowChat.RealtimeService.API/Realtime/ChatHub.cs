using System.Security.Claims;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.RegisterRealtimeConnection;
using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.UnregisterRealtimeConnection;
using FlowChat.RealtimeService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FlowChat.RealtimeService.Api.Realtime;

[Authorize]
public sealed class ChatHub(
    ILogger<ChatHub> logger,
    IMediator mediator,
    IPresenceInternalApiClient presenceInternalApiClient,
    IRealtimeGroupMembershipRepository realtimeGroupMembershipRepository) : Hub<IRealtimeClient>
{
    private const string SubjectClaimType = "sub";
    private readonly ILogger<ChatHub> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IPresenceInternalApiClient _presenceInternalApiClient = presenceInternalApiClient
        ?? throw new ArgumentNullException(nameof(presenceInternalApiClient));
    private readonly IRealtimeGroupMembershipRepository _realtimeGroupMembershipRepository = realtimeGroupMembershipRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipRepository));

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
        var registeredConnection = false;
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.ForUser(userId.Value));
            addedToGroup = true;

            await JoinConversationGroupsAsync(userId.Value);

            var result = await _mediator.Send(
                new RegisterRealtimeConnectionCommand(userId.Value, Context.ConnectionId),
                Context.ConnectionAborted);
            if (result.IsFailure)
            {
                // Client-initiated disconnects during registration are expected (e.g. React StrictMode remounts), not real failures.
                if (Context.ConnectionAborted.IsCancellationRequested)
                {
                    _logger.LogWarning(
                        "Realtime connection {ConnectionId} for user {UserId} was aborted by the client before registration completed.",
                        Context.ConnectionId,
                        userId.Value);
                }
                else
                {
                    _logger.LogError(
                        "Failed to register realtime connection {ConnectionId} for user {UserId}: {ErrorMessage}",
                        Context.ConnectionId,
                        userId.Value,
                        result.Error.ErrorMessage);
                }

                await CleanupFailedConnectionAsync(userId.Value, addedToGroup);
                Context.Abort();
                return;
            }

            registeredConnection = true;
            await _presenceInternalApiClient.InitializePresenceStatusAsync(userId.Value, Context.ConnectionAborted);
            await base.OnConnectedAsync();
        }
        catch (OperationCanceledException) when (Context.ConnectionAborted.IsCancellationRequested)
        {
            await CleanupFailedConnectionAsync(userId.Value, addedToGroup, registeredConnection);
            Context.Abort();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to register realtime connection {ConnectionId} for user {UserId}.", Context.ConnectionId, userId.Value);
            await CleanupFailedConnectionAsync(userId.Value, addedToGroup, registeredConnection);
            Context.Abort();
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var result = await _mediator.Send(
                new UnregisterRealtimeConnectionCommand(Context.ConnectionId),
                CancellationToken.None);
            if (result.IsFailure)
            {
                _logger.LogError(
                    "Failed to unregister realtime connection {ConnectionId}: {ErrorMessage}",
                    Context.ConnectionId,
                    result.Error.ErrorMessage);
            }
        }
        catch (Exception unregisterException)
        {
            _logger.LogError(unregisterException, "Failed to unregister realtime connection {ConnectionId}.", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task JoinConversationGroupsAsync(Guid userId)
    {
        try
        {
            var memberships = await _realtimeGroupMembershipRepository.GetByUserIdAsync(userId, Context.ConnectionAborted);
            var conversationIds = memberships
                .Where(membership => membership.GroupType == RealtimeGroupType.Conversation)
                .Select(membership => membership.ResourceId);

            foreach (var conversationId in conversationIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.ForConversation(conversationId));
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to join conversation groups for realtime connection {ConnectionId} and user {UserId}.",
                Context.ConnectionId,
                userId);
        }
    }

    private Guid? ResolveUserId()
    {
        var value = Context.User?.FindFirstValue(SubjectClaimType)
            ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private async Task CleanupFailedConnectionAsync(Guid userId, bool addedToGroup, bool registeredConnection = false)
    {
        if (registeredConnection)
        {
            try
            {
                await _mediator.Send(
                    new UnregisterRealtimeConnectionCommand(Context.ConnectionId),
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to unregister realtime connection {ConnectionId} after connection failure.", Context.ConnectionId);
            }
        }

        try
        {
            if (!addedToGroup)
            {
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.ForUser(userId));
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to remove realtime connection {ConnectionId} from user group after connection failure.", Context.ConnectionId);
        }
    }
}
