using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Domain.Enums;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.RegisterRealtimeConnection;

// Runs group membership, connection registration and presence initialization in parallel and keeps them consistent on failure, logging structured failure context (user/connection ids) that LoggingPipelineBehaviour cannot see.
// Compensation is best-effort: if RegisterAsync fails while InitializePresenceStatusAsync happens to succeed in parallel, there is no mutation result to tell whether this was the user's first connection, so the presence status is left as-is rather than risking deleting a status still owned by another active connection.
public sealed class RegisterRealtimeConnectionCommandHandler(
    IRealtimeGroupManager realtimeGroupManager,
    IRealtimeGroupMembershipReadModelRepository realtimeGroupMembershipReadModelRepository,
    IRealtimeConnectionRegistry realtimeConnectionRegistry,
    IPresenceInternalApiClient presenceInternalApiClient,
    ILogger<RegisterRealtimeConnectionCommandHandler> logger)
    : ICommandHandler<RegisterRealtimeConnectionCommand, Unit>
{
    private readonly IRealtimeGroupManager _realtimeGroupManager = realtimeGroupManager
        ?? throw new ArgumentNullException(nameof(realtimeGroupManager));
    private readonly IRealtimeGroupMembershipReadModelRepository _realtimeGroupMembershipReadModelRepository = realtimeGroupMembershipReadModelRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipReadModelRepository));
    private readonly IRealtimeConnectionRegistry _realtimeConnectionRegistry = realtimeConnectionRegistry
        ?? throw new ArgumentNullException(nameof(realtimeConnectionRegistry));
    private readonly IPresenceInternalApiClient _presenceInternalApiClient = presenceInternalApiClient
        ?? throw new ArgumentNullException(nameof(presenceInternalApiClient));
    private readonly ILogger<RegisterRealtimeConnectionCommandHandler> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task<FlowChatResult<Unit>> Handle(RegisterRealtimeConnectionCommand request, CancellationToken cancellationToken)
    {
        var addUserGroupTask = _realtimeGroupManager.AddToUserGroupAsync(request.ConnectionId!, request.UserId, cancellationToken);
        var joinConversationGroupsTask = JoinConversationGroupsAsync(request.ConnectionId!, request.UserId, cancellationToken);
        var registerTask = _realtimeConnectionRegistry.RegisterAsync(request.UserId, request.ConnectionId!, cancellationToken);
        var presenceTask = _presenceInternalApiClient.InitializePresenceStatusAsync(request.UserId, cancellationToken);

        try
        {
            await Task.WhenAll(addUserGroupTask, joinConversationGroupsTask, registerTask, presenceTask);

            return FlowChatResult<Unit>.Success(Unit.Value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await CompensateAsync(request, addUserGroupTask, registerTask, presenceTask);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to register realtime connection for user {UserId} and connection {ConnectionId}.",
                request.UserId,
                request.ConnectionId);

            await CompensateAsync(request, addUserGroupTask, registerTask, presenceTask);

            return FlowChatResult<Unit>.Failure(DomainError.UnExpected("Failed to register realtime connection."));
        }
    }

    private async Task JoinConversationGroupsAsync(string connectionId, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var memberships = await _realtimeGroupMembershipReadModelRepository.GetByUserIdAsync(userId, cancellationToken);
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

    private async Task CompensateAsync(
        RegisterRealtimeConnectionCommand request,
        Task addUserGroupTask,
        Task<RealtimeConnectionMutationResult> registerTask,
        Task presenceTask)
    {
        await _realtimeConnectionRegistry.UnregisterAsync(request.ConnectionId!, CancellationToken.None);

        if (addUserGroupTask.IsCompletedSuccessfully)
        {
            await _realtimeGroupManager.RemoveFromUserGroupAsync(request.ConnectionId!, request.UserId, CancellationToken.None);
        }

        if (presenceTask.IsCompletedSuccessfully
            && registerTask.IsCompletedSuccessfully
            && registerTask.Result.IsFirstConnectionForUser)
        {
            try
            {
                await _presenceInternalApiClient.DeletePresenceStatusAsync(request.UserId, CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to delete presence status for user {UserId} during compensation.",
                    request.UserId);
            }
        }
    }
}
