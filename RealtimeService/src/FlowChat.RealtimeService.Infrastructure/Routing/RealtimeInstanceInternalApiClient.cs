using System.Net.Http.Json;
using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.RealtimeService.Infrastructure.Routing;

public sealed class RealtimeInstanceInternalApiClient(HttpClient httpClient)
    : FlowChatHttpClientBase(httpClient), IRealtimeInstanceInternalApiClient
{
    public const string ApiKeyHeaderName = "X-Internal-Api-Key";
    private const string MessageReceivedPath = "/internal/realtime/messages/direct";
    private const string PresenceChangedPath = "/internal/realtime/presence/direct";
    private const string GroupConversationChangedPath = "/internal/realtime/group-conversations/changed/direct";
    private const string GroupConversationParticipantsAddedPath = "/internal/realtime/group-conversations/participants-added/direct";
    private const string GroupConversationParticipantsRemovedPath = "/internal/realtime/group-conversations/participants-removed/direct";
    private const string DuetConversationCreatedPath = "/internal/realtime/duet-conversations/created/direct";

    protected override string ClientDisplayName => "Realtime API";

    public Task PublishMessageAsync(
        Uri baseAddress,
        ChatMessageParam notification,
        CancellationToken cancellationToken) =>
        PostAsync(
            baseAddress,
            MessageReceivedPath,
            new PublishMessageRequest(
                notification.MessageId,
                notification.ConversationId,
                notification.SenderUserId,
                notification.SenderDisplayName,
                notification.Text,
                notification.SequenceNum,
                notification.SentAtUtc,
                notification.DeliveredAtUtc),
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
                notification.CreatedByUserId),
            cancellationToken);

    public Task PublishGroupConversationParticipantsAddedAsync(
        Uri baseAddress,
        GroupConversationParticipantsAddedParam notification,
        CancellationToken cancellationToken) =>
        PostAsync(
            baseAddress,
            GroupConversationParticipantsAddedPath,
            new PublishGroupConversationParticipantsAddedRequest(
                notification.ConversationId,
                notification.ParticipantUserIds),
            cancellationToken);

    public Task PublishGroupConversationParticipantsRemovedAsync(
        Uri baseAddress,
        GroupConversationParticipantsRemovedParam notification,
        CancellationToken cancellationToken) =>
        PostAsync(
            baseAddress,
            GroupConversationParticipantsRemovedPath,
            new PublishGroupConversationParticipantsRemovedRequest(
                notification.ConversationId,
                notification.ParticipantUserIds),
            cancellationToken);

    public Task PublishDuetConversationCreatedAsync(
        Uri baseAddress,
        DuetConversationCreatedParam notification,
        CancellationToken cancellationToken) =>
        PostAsync(
            baseAddress,
            DuetConversationCreatedPath,
            new PublishDuetConversationCreatedRequest(
                notification.ConversationId,
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
        long SequenceNum,
        DateTimeOffset SentAtUtc,
        DateTimeOffset DeliveredAtUtc);

    private sealed record PublishPresenceChangeRequest(
        Guid UserId,
        PresenceStatus Status,
        DateTimeOffset ChangedAtUtc,
        IReadOnlyCollection<Guid> RecipientUserIds);

    private sealed record PublishGroupConversationChangedRequest(
        Guid ConversationId,
        int Type,
        string? Name,
        Guid CreatedByUserId);

    private sealed record PublishGroupConversationParticipantsAddedRequest(
        Guid ConversationId,
        IReadOnlyCollection<Guid> ParticipantUserIds);

    private sealed record PublishGroupConversationParticipantsRemovedRequest(
        Guid ConversationId,
        IReadOnlyCollection<Guid> ParticipantUserIds);

    private sealed record PublishDuetConversationCreatedRequest(
        Guid ConversationId,
        IReadOnlyCollection<Guid> ParticipantUserIds);
}
