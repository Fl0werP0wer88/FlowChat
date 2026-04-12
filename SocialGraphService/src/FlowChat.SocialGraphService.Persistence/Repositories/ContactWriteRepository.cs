using FlowChat.Shared.Persistance;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities.Contact;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class ContactWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<Contact>(dbContext), IContactWriteRepository
{
    public Task<bool> ExistsAsync(Guid ownerUserId, Guid contactUserId, CancellationToken cancellationToken = default)
    {
        return DbContext.Set<Contact>()
            .AnyAsync(
                contact => contact.OwnerUserId == ownerUserId && contact.ContactUserId == contactUserId,
                cancellationToken);
    }

    public Task<Contact?> GetByOwnerAndContactAsync(
        Guid ownerUserId,
        Guid contactUserId,
        CancellationToken cancellationToken = default)
    {
        return DbContext.Set<Contact>()
            .FirstOrDefaultAsync(
                contact => contact.OwnerUserId == ownerUserId && contact.ContactUserId == contactUserId,
                cancellationToken);
    }
}

