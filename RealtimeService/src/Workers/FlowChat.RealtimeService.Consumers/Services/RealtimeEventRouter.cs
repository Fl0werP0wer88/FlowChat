using FlowChat.RealtimeService.Consumers.Realtime.Contracts;
using FlowChat.RealtimeService.Redis.RealtimeConnections;

namespace FlowChat.RealtimeService.Consumers.Services;

public sealed class RealtimeEventRouter(
    IUserInstanceRoutingReader userInstanceRoutingReader,
    IRealtimeInstanceAddressResolver instanceAddressResolver,
    IRealtimeInternalApiClient realtimeInternalApiClient)
    : IRealtimeEventRouter
{
    private readonly IUserInstanceRoutingReader _userInstanceRoutingReader = userInstanceRoutingReader
        ?? throw new ArgumentNullException(nameof(userInstanceRoutingReader));
    private readonly IRealtimeInstanceAddressResolver _instanceAddressResolver = instanceAddressResolver
        ?? throw new ArgumentNullException(nameof(instanceAddressResolver));
    private readonly IRealtimeInternalApiClient _realtimeInternalApiClient = realtimeInternalApiClient
        ?? throw new ArgumentNullException(nameof(realtimeInternalApiClient));

    public Task PublishMessageAsync(PublishMessageRequest request, CancellationToken cancellationToken) =>
        RouteAsync(
            request.RecipientUserIds,
            cancellationToken,
            routedRecipients => _realtimeInternalApiClient.PublishMessageAsync(
                _instanceAddressResolver.Resolve(routedRecipients.InstanceId),
                CloneMessageRequest(request, routedRecipients.UserIds),
                cancellationToken));

    public Task PublishPresenceChangeAsync(PublishPresenceChangeRequest request, CancellationToken cancellationToken) =>
        RouteAsync(
            request.RecipientUserIds,
            cancellationToken,
            routedRecipients => _realtimeInternalApiClient.PublishPresenceChangeAsync(
                _instanceAddressResolver.Resolve(routedRecipients.InstanceId),
                ClonePresenceChangeRequest(request, routedRecipients.UserIds),
                cancellationToken));

    private async Task RouteAsync(
        IReadOnlyCollection<Guid> recipientUserIds,
        CancellationToken cancellationToken,
        Func<RoutedRecipients, Task> publishAsync)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var recipientsByInstance = await GetRecipientsByInstanceAsync(recipientUserIds, cancellationToken);
        if (recipientsByInstance.Count == 0)
        {
            return;
        }

        await Task.WhenAll(recipientsByInstance.Select(publishAsync));
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

    private static PublishMessageRequest CloneMessageRequest(PublishMessageRequest request, IReadOnlyCollection<Guid> recipientUserIds) =>
        new()
        {
            MessageId = request.MessageId,
            ConversationId = request.ConversationId,
            SenderUserId = request.SenderUserId,
            SenderDisplayName = request.SenderDisplayName,
            Text = request.Text,
            SentAtUtc = request.SentAtUtc,
            RecipientUserIds = recipientUserIds
        };

    private static PublishPresenceChangeRequest ClonePresenceChangeRequest(
        PublishPresenceChangeRequest request,
        IReadOnlyCollection<Guid> recipientUserIds) =>
        new()
        {
            UserId = request.UserId,
            Status = request.Status,
            ChangedAtUtc = request.ChangedAtUtc,
            RecipientUserIds = recipientUserIds
        };

    private sealed record RoutedRecipients(string InstanceId, IReadOnlyCollection<Guid> UserIds);
}
