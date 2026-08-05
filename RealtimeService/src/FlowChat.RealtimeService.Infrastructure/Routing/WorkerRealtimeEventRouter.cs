using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Redis.RealtimeConnections;

namespace FlowChat.RealtimeService.Infrastructure.Routing;

//ToDo: Tutaj brakuje exactly one delivery. Rozważyć wprowadzenie  może topic per instance. Może też jednak cos pokombinowac z Redis fan out?
public sealed class WorkerRealtimeEventRouter(
    IUserInstanceRoutingReader userInstanceRoutingReader,
    IRealtimeInstanceAddressResolver instanceAddressResolver,
    IRealtimeInstanceInternalApiClient realtimeInstanceInternalApiClient)
    : IRealtimeEventRouter
{
    private readonly IUserInstanceRoutingReader _userInstanceRoutingReader = userInstanceRoutingReader
        ?? throw new ArgumentNullException(nameof(userInstanceRoutingReader));
    private readonly IRealtimeInstanceAddressResolver _instanceAddressResolver = instanceAddressResolver
        ?? throw new ArgumentNullException(nameof(instanceAddressResolver));
    private readonly IRealtimeInstanceInternalApiClient _realtimeInstanceInternalApiClient = realtimeInstanceInternalApiClient
        ?? throw new ArgumentNullException(nameof(realtimeInstanceInternalApiClient));

    public Task RouteMessageAsync(ChatMessageParam notification, CancellationToken cancellationToken) =>
        BroadcastAsync(
            notification.RecipientUserIds,
            instanceUrl => _realtimeInstanceInternalApiClient.PublishMessageAsync(
                instanceUrl,
                notification,
                cancellationToken),
            cancellationToken);

    public Task RoutePresenceChangeAsync(PresenceChangedParam notification, CancellationToken cancellationToken) =>
        RouteAsync(
            notification.RecipientUserIds,
            (instanceUrl, userIds) => _realtimeInstanceInternalApiClient.PublishPresenceChangeAsync(
                instanceUrl,
                notification with { RecipientUserIds = userIds },
                cancellationToken),
            cancellationToken);

    public Task RouteGroupConversationChangedAsync(GroupConversationChangedParam notification, CancellationToken cancellationToken) =>
        BroadcastAsync(
            notification.ParticipantUserIds,
            instanceUrl => _realtimeInstanceInternalApiClient.PublishGroupConversationChangedAsync(
                instanceUrl,
                notification,
                cancellationToken),
            cancellationToken);

    public Task RouteConversationParticipantsAddedAsync(ConversationParticipantsAddedParam notification, CancellationToken cancellationToken) =>
        BroadcastAsync(
            notification.RecipientUserIds,
            instanceUrl => _realtimeInstanceInternalApiClient.PublishConversationParticipantsAddedAsync(
                instanceUrl,
                notification,
                cancellationToken),
            cancellationToken);

    public Task RouteConversationParticipantsRemovedAsync(ConversationParticipantsRemovedParam notification, CancellationToken cancellationToken) =>
        BroadcastAsync(
            notification.RecipientUserIds,
            instanceUrl => _realtimeInstanceInternalApiClient.PublishConversationParticipantsRemovedAsync(
                instanceUrl,
                notification,
                cancellationToken),
            cancellationToken);

    private async Task RouteAsync(
        IReadOnlyCollection<Guid> recipientUserIds,
        Func<Uri, IReadOnlyCollection<Guid>, Task> publishAsync,
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
            var instanceUrl = _instanceAddressResolver.Resolve(routedRecipients.InstanceId);
            tasks.Add(publishAsync(instanceUrl, routedRecipients.UserIds));
        }

        await Task.WhenAll(tasks);
    }

    private async Task BroadcastAsync(
        IReadOnlyCollection<Guid> recipientUserIds,
        Func<Uri, Task> publishAsync,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var instanceIds = await GetInstanceIdsAsync(recipientUserIds, cancellationToken);
        if (instanceIds.Count == 0)
        {
            return;
        }

        var tasks = instanceIds.Select(instanceId => publishAsync(_instanceAddressResolver.Resolve(instanceId)));
        await Task.WhenAll(tasks);
    }

    private async Task<IReadOnlyCollection<string>> GetInstanceIdsAsync(
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

        return instanceIdsByUser.Values
            .SelectMany(static instanceIds => instanceIds)
            .Where(static instanceId => !string.IsNullOrWhiteSpace(instanceId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
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
