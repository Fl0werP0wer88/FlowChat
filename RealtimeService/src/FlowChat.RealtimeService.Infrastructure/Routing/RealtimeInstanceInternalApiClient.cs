using System.Net.Http.Json;
using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.RealtimeService.Infrastructure.Routing;

public sealed class RealtimeInstanceInternalApiClient(HttpClient httpClient)
    : ConsumerHttpClientBase(httpClient), IRealtimeInstanceInternalApiClient
{
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string ReceiveMessagePath = "/internal/realtime/messages/direct";
    private const string PresenceChangedPath = "/internal/realtime/presence/direct";

    protected override string ClientDisplayName => "Realtime API";

    public Task PublishMessageAsync(
        Uri baseAddress,
        ChatMessageNotification notification,
        IReadOnlyCollection<Guid> recipientUserIds,
        CancellationToken cancellationToken) =>
        PostAsync(
            baseAddress,
            ReceiveMessagePath,
            new PublishMessageRequest(
                notification.MessageId,
                notification.ConversationId,
                notification.SenderUserId,
                notification.SenderDisplayName,
                notification.Text,
                notification.SentAtUtc,
                recipientUserIds),
            cancellationToken);

    public Task PublishPresenceChangeAsync(
        Uri baseAddress,
        PresenceChangedNotification notification,
        IReadOnlyCollection<Guid> recipientUserIds,
        CancellationToken cancellationToken) =>
        PostAsync(
            baseAddress,
            PresenceChangedPath,
            new PublishPresenceChangeRequest(
                notification.UserId,
                notification.Status,
                notification.ChangedAtUtc,
                recipientUserIds),
            cancellationToken);

    private async Task PostAsync<TRequest>(Uri baseAddress, string path, TRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);

        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(baseAddress, path))
        {
            Content = JsonContent.Create(request)
        };

        await SendAsync(message, cancellationToken);
    }

    private sealed record PublishMessageRequest(
        Guid MessageId,
        Guid ConversationId,
        Guid SenderUserId,
        string SenderDisplayName,
        string Text,
        DateTimeOffset SentAtUtc,
        IReadOnlyCollection<Guid> RecipientUserIds);

    private sealed record PublishPresenceChangeRequest(
        Guid UserId,
        PresenceStatus Status,
        DateTimeOffset ChangedAtUtc,
        IReadOnlyCollection<Guid> RecipientUserIds);
}
