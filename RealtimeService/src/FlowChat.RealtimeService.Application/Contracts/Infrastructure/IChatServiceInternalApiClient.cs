namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public interface IChatServiceInternalApiClient
{
    Task<long> SetChatMessageSequenceNumberAsync(
        Guid messageId,
        Guid conversationId,
        CancellationToken cancellationToken);

    Task MarkChatMessageAsDeliveredAsync(
        Guid messageId,
        Guid conversationId,
        DateTimeOffset deliveredAtUtc,
        CancellationToken cancellationToken);
}
