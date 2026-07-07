using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Domain.Enums;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.RegisterRealtimeConnection;

// Keeps user/conversation group membership, connection registration and presence initialization consistent, and logs structured failure context (user/connection ids) that LoggingPipelineBehaviour cannot see
public sealed class RegisterRealtimeConnectionCommandHandler(
    IRealtimeGroupManager realtimeGroupManager,
    IRealtimeGroupMembershipRepository realtimeGroupMembershipRepository,
    IRealtimeConnectionRegistry realtimeConnectionRegistry,
    IPresenceInternalApiClient presenceInternalApiClient,
    ILogger<RegisterRealtimeConnectionCommandHandler> logger)
    : ICommandHandler<RegisterRealtimeConnectionCommand, Unit>
{
    private readonly IRealtimeGroupManager _realtimeGroupManager = realtimeGroupManager
        ?? throw new ArgumentNullException(nameof(realtimeGroupManager));
    private readonly IRealtimeGroupMembershipRepository _realtimeGroupMembershipRepository = realtimeGroupMembershipRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipRepository));
    private readonly IRealtimeConnectionRegistry _realtimeConnectionRegistry = realtimeConnectionRegistry
        ?? throw new ArgumentNullException(nameof(realtimeConnectionRegistry));
    private readonly IPresenceInternalApiClient _presenceInternalApiClient = presenceInternalApiClient
        ?? throw new ArgumentNullException(nameof(presenceInternalApiClient));
    private readonly ILogger<RegisterRealtimeConnectionCommandHandler> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task<FlowChatResult<Unit>> Handle(RegisterRealtimeConnectionCommand request, CancellationToken cancellationToken)
    {
        var addedToUserGroup = false;
        try
        {
            await _realtimeGroupManager.AddToUserGroupAsync(request.ConnectionId!, request.UserId, cancellationToken);
            addedToUserGroup = true;

            await JoinConversationGroupsAsync(request.ConnectionId!, request.UserId, cancellationToken);

            await _realtimeConnectionRegistry.RegisterAsync(request.UserId, request.ConnectionId!, cancellationToken);

            await _presenceInternalApiClient.InitializePresenceStatusAsync(request.UserId, cancellationToken);

            return FlowChatResult<Unit>.Success(Unit.Value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await CompensateAsync(request, addedToUserGroup);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to register realtime connection for user {UserId} and connection {ConnectionId}.",
                request.UserId,
                request.ConnectionId);

            await CompensateAsync(request, addedToUserGroup);

            return FlowChatResult<Unit>.Failure(DomainError.UnExpected("Failed to register realtime connection."));
        }
    }

    private async Task JoinConversationGroupsAsync(string connectionId, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var memberships = await _realtimeGroupMembershipRepository.GetByUserIdAsync(userId, cancellationToken);
            var conversationIds = memberships
                .Where(membership => membership.GroupType == RealtimeGroupType.Conversation)
                .Select(membership => membership.ResourceId);

            foreach (var conversationId in conversationIds)
            {
                await _realtimeGroupManager.AddToConversationGroupAsync(connectionId, conversationId, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to join conversation groups for realtime connection {ConnectionId} and user {UserId}.",
                connectionId,
                userId);
        }
    }

    private async Task CompensateAsync(RegisterRealtimeConnectionCommand request, bool addedToUserGroup)
    {
        await _realtimeConnectionRegistry.UnregisterAsync(request.ConnectionId!, CancellationToken.None);

        if (addedToUserGroup)
        {
            await _realtimeGroupManager.RemoveFromUserGroupAsync(request.ConnectionId!, request.UserId, CancellationToken.None);
        }
    }
}
