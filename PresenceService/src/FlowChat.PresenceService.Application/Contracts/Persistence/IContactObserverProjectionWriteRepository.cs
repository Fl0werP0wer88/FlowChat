namespace FlowChat.PresenceService.Application.Contracts.Persistence;

public interface IContactObserverProjectionWriteRepository
{
    Task<bool> DeleteAsync(Guid observedUserId, Guid observerUserId, CancellationToken cancellationToken = default);
}
