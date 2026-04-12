namespace FlowChat.PresenceService.Application.Contracts.Persistence;

public interface IContactObserverProjectionReadRepository
{
    Task<IReadOnlyCollection<Guid>> GetObserverUserIdsAsync(Guid observedUserId, CancellationToken cancellationToken = default);
}
