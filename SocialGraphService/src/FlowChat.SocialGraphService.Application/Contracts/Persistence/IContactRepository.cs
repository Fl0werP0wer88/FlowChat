using FlowChat.SocialGraphService.Domain.Entities;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IContactRepository
{
    Task<Contact> AddAsync(Contact entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Contact entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Contact entity, CancellationToken cancellationToken = default);
}
