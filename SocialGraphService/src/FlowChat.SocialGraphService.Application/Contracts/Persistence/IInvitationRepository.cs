using FlowChat.SocialGraphService.Domain.Entities;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IInvitationRepository
{
    Task<Invitation> AddAsync(Invitation entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Invitation entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Invitation entity, CancellationToken cancellationToken = default);
}
