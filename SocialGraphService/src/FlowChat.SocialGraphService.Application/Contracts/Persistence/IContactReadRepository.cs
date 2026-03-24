using FlowChat.SocialGraphService.Application.Contacts.Queries.GetContactsForUser;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IContactReadRepository : IReadRepository<ContactDto>
{
    Task<IReadOnlyList<ContactDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
