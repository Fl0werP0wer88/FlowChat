using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Redis.Configuration.Settings;
using FlowChat.RealtimeService.Redis.RealtimeConnections;
using Microsoft.Extensions.Options;

namespace FlowChat.RealtimeService.Infrastructure.Routing;

public sealed class RealtimeEventRouter(
    IUserInstanceRoutingReader userInstanceRoutingReader,
    IRealtimeInstanceAddressResolver instanceAddressResolver,
    IRealtimeInstanceInternalApiClient realtimeInstanceInternalApiClient,
    IRealtimeClientDispatcher realtimeClientDispatcher,
    IOptions<RealtimeConnectionsSettingsSection> realtimeConnectionsSettings)
    : IRealtimeEventRouter
{
    private readonly IUserInstanceRoutingReader _userInstanceRoutingReader = userInstanceRoutingReader
        ?? throw new ArgumentNullException(nameof(userInstanceRoutingReader));
    private readonly IRealtimeInstanceAddressResolver _instanceAddressResolver = instanceAddressResolver
        ?? throw new ArgumentNullException(nameof(instanceAddressResolver));
    private readonly IRealtimeInstanceInternalApiClient _realtimeInstanceInternalApiClient = realtimeInstanceInternalApiClient
        ?? throw new ArgumentNullException(nameof(realtimeInstanceInternalApiClient));
    private readonly IRealtimeClientDispatcher _realtimeClientDispatcher = realtimeClientDispatcher
        ?? throw new ArgumentNullException(nameof(realtimeClientDispatcher));
    private readonly string _ownInstanceId = realtimeConnectionsSettings?.Value.InstanceId
        ?? throw new ArgumentNullException(nameof(realtimeConnectionsSettings));

    public Task RouteMessageAsync(ChatMessageNotification notification, CancellationToken cancellationToken) =>
        RouteAsync(
            notification.RecipientUserIds,
            localRecipients => _realtimeClientDispatcher.ReceiveMessageAsync(
                notification with { RecipientUserIds = localRecipients },
                cancellationToken),
            (instanceUrl, userIds) => _realtimeInstanceInternalApiClient.PublishMessageAsync(
                instanceUrl,
                notification with { RecipientUserIds = userIds },
                cancellationToken),
            cancellationToken);

    public Task RoutePresenceChangeAsync(PresenceChangedNotification notification, CancellationToken cancellationToken) =>
        RouteAsync(
            notification.RecipientUserIds,
            localRecipients => _realtimeClientDispatcher.PresenceChangedAsync(
                notification with { RecipientUserIds = localRecipients },
                cancellationToken),
            (instanceUrl, userIds) => _realtimeInstanceInternalApiClient.PublishPresenceChangeAsync(
                instanceUrl,
                notification with { RecipientUserIds = userIds },
                cancellationToken),
            cancellationToken);

    private async Task RouteAsync(
        IReadOnlyCollection<Guid> recipientUserIds,
        Func<IReadOnlyCollection<Guid>, Task> dispatchLocalAsync,
        Func<Uri, IReadOnlyCollection<Guid>, Task> publishRemoteAsync,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var recipientsByInstance = await GetRecipientsByInstanceAsync(recipientUserIds, cancellationToken);
        if (recipientsByInstance.Count == 0)
        {
            return;
        }

        List<Task> tasks = [];
        foreach (var routedRecipients in recipientsByInstance)
        {
            if (string.Equals(routedRecipients.InstanceId, _ownInstanceId, StringComparison.Ordinal))
            {
                tasks.Add(dispatchLocalAsync(routedRecipients.UserIds));
            }
            else
            {
                var instanceUrl = _instanceAddressResolver.Resolve(routedRecipients.InstanceId);
                tasks.Add(publishRemoteAsync(instanceUrl, routedRecipients.UserIds));
            }
        }

        await Task.WhenAll(tasks);
    }

    private async Task<IReadOnlyCollection<RoutedRecipients>> GetRecipientsByInstanceAsync(
        IReadOnlyCollection<Guid> recipientUserIds,
        CancellationToken cancellationToken)
    {
        var filteredRecipientIds = recipientUserIds
            .Where(static userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();
        if (filteredRecipientIds.Length == 0)
        {
            return [];
        }

        var instanceIdsByUser = await _userInstanceRoutingReader.GetInstanceIdsByUserAsync(filteredRecipientIds, cancellationToken);
        Dictionary<string, HashSet<Guid>> recipientsByInstance = new(StringComparer.Ordinal);

        foreach (var (userId, instanceIds) in instanceIdsByUser)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var instanceId in instanceIds.Where(static instanceId => !string.IsNullOrWhiteSpace(instanceId)))
            {
                if (!recipientsByInstance.TryGetValue(instanceId, out var recipients))
                {
                    recipients = [];
                    recipientsByInstance[instanceId] = recipients;
                }

                recipients.Add(userId);
            }
        }

        return recipientsByInstance
            .Select(pair => new RoutedRecipients(pair.Key, pair.Value.ToArray()))
            .ToArray();
    }

    private sealed record RoutedRecipients(string InstanceId, IReadOnlyCollection<Guid> UserIds);
}
