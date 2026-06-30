using System.Net.Http.Json;
using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.RealtimeService.Infrastructure.Routing;

public sealed class RealtimeInstanceInternalApiClient(HttpClient httpClient)
    : FlowChatHttpClientBase(httpClient), IRealtimeInstanceInternalApiClient
{
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string ReceiveMessagePath = "/internal/realtime/messages/direct";
    private const string PresenceChangedPath = "/internal/realtime/presence/direct";
    private const string GroupConversationChangedPath = "/internal/realtime/group-conversations/changed/direct";

    protected override string ClientDisplayName => "Realtime API";

    public Task PublishMessageAsync(
        Uri baseAddress,
        ChatMessageParam notification,
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
                notification.DeliveredAtUtc,
                notification.RecipientUserIds),
            cancellationToken);

    public Task PublishPresenceChangeAsync(
        Uri baseAddress,
        PresenceChangedParam notification,
        CancellationToken cancellationToken) =>
        PostAsync(
            baseAddress,
            PresenceChangedPath,
            new PublishPresenceChangeRequest(
                notification.UserId,
                notification.Status,
                notification.ChangedAtUtc,
                notification.RecipientUserIds),
            cancellationToken);

    public Task PublishGroupConversationChangedAsync(
        Uri baseAddress,
        GroupConversationChangedParam notification,
        CancellationToken cancellationToken) =>
        PostAsync(
            baseAddress,
            GroupConversationChangedPath,
            new PublishGroupConversationChangedRequest(
                notification.ConversationId,
                notification.Type,
                notification.Name,
                notification.CreatedByUserId,
                notification.ParticipantUserIds),
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
        DateTimeOffset DeliveredAtUtc,
        IReadOnlyCollection<Guid> RecipientUserIds);

    private sealed record PublishPresenceChangeRequest(
        Guid UserId,
        PresenceStatus Status,
        DateTimeOffset ChangedAtUtc,
        IReadOnlyCollection<Guid> RecipientUserIds);

    private sealed record PublishGroupConversationChangedRequest(
        Guid ConversationId,
        int Type,
        string? Name,
        Guid CreatedByUserId,
        IReadOnlyCollection<Guid> ParticipantUserIds);
}
