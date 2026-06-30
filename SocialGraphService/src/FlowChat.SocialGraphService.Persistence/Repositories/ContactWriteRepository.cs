using FlowChat.Shared.Persistance;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities.Contact;
using Microsoft.EntityFrameworkCore;
using UserProfileMarker = FlowChat.SocialGraphService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class ContactWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<Contact>(dbContext), IContactWriteRepository
{
    public Task<bool> ExistsAsync(
        Id<UserProfileMarker> ownerUserId,
        Id<UserProfileMarker> contactUserId,
        CancellationToken cancellationToken = default)
    {
        return DbContext.Set<Contact>()
            .AnyAsync(
                contact => contact.OwnerUserId == ownerUserId && contact.ContactUserId == contactUserId,
                cancellationToken);
    }

    public Task<Contact?> GetByOwnerAndContactAsync(
        Id<UserProfileMarker> ownerUserId,
        Id<UserProfileMarker> contactUserId,
        CancellationToken cancellationToken = default)
    {
        return DbContext.Set<Contact>()
            .FirstOrDefaultAsync(
                contact => contact.OwnerUserId == ownerUserId && contact.ContactUserId == contactUserId,
                cancellationToken);
    }
}

