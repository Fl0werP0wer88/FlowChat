using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Domain.Entities.Contact;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IContactWriteRepository : IWriteRepository<Contact>
{
    Task<bool> ExistsAsync(Guid ownerUserId, Guid contactUserId, CancellationToken cancellationToken = default);
}

