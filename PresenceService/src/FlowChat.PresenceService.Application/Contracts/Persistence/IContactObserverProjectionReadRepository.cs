namespace FlowChat.PresenceService.Application.Contracts.Persistence;

public interface IContactObserverProjectionReadRepository
{
    Task<IReadOnlyCollection<Guid>> GetObserverUserIdsAsync(Guid observedUserId, CancellationToken cancellationToken = default);

    /// <summary>Returns all userIds that observerUserId is watching (i.e. the observer's contact list).</summary>
    Task<IReadOnlyCollection<Guid>> GetObservedUserIdsAsync(Guid observerUserId, CancellationToken cancellationToken = default);
}
