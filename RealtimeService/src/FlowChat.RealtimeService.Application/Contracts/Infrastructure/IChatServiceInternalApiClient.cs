namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public interface IChatServiceInternalApiClient
{
    Task MarkChatMessageAsDeliveredAsync(
        Guid messageId,
        Guid conversationId,
        DateTimeOffset deliveredAtUtc,
        CancellationToken cancellationToken);
}
