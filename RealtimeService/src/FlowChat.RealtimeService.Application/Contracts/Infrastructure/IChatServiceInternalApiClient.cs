namespace FlowChat.RealtimeService.Application.Contracts.Infrastructure;

public interface IChatServiceInternalApiClient
{
    Task MarkMessageProcessedAsync(Guid messageId, Guid conversationId, CancellationToken cancellationToken);
}
