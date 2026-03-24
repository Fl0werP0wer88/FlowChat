using FlowChat.SocialGraphService.Domain.Entities;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IContactWriteRepository
{
    Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Contact> AddAsync(Contact entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Contact entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Contact entity, CancellationToken cancellationToken = default);
}
