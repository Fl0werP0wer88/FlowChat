using FlowChat.PresenceService.Application.Features.ContactObserverProjections;

namespace FlowChat.PresenceService.Application.Contracts.Persistence;

public interface IContactObserverProjectionWriteRepository
{
    Task InsertAsync(ContactObserverProjectionDto projection, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid observedUserId, Guid observerUserId, CancellationToken cancellationToken = default);
}
