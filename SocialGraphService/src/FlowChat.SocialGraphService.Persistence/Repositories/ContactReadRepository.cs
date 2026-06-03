using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.Contact.Queries.GetContactsForUser;
using FlowChat.SocialGraphService.Persistence.Entities;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class ContactReadRepository(AppDbContext dbContext) : ReadRepositoryBase, IContactReadRepository
{
    private static readonly Expression<Func<ContactReadEntity, ContactDto>> ContactDtoProjection = x => new(
        x.Id,
        x.OwnerUserId,
        x.ContactUserId,
        x.DisplayName,
        x.FirstName,
        x.LastName,
        x.PhoneNumber,
        x.EmailAddress,
        x.IsBlocked);

    public async Task<ContactDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Query()
            .Where(x => x.Id == id)
            .Select(ContactDtoProjection)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContactDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Query()
            .Select(ContactDtoProjection)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContactDto>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await Query()
            .Where(x => x.OwnerUserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(ContactDtoProjection)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<ContactReadEntity> Query() => Active(dbContext.ContactReads);
}
