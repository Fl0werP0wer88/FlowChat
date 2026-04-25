namespace FlowChat.GatewayService.Api.Services;

public interface IChatServiceClient
{
    Task<IReadOnlyDictionary<Guid, Guid>> GetDuetConversationIdsAsync(
        IReadOnlyList<Guid> partnerUserIds,
        CancellationToken cancellationToken);
}
