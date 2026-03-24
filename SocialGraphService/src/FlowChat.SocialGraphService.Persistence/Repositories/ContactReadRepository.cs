using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Contacts.Queries.GetContactsForUser;
using FlowChat.SocialGraphService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class ContactReadRepository(AppDbContext dbContext)
    : ReadRepositoryBase<Contact, ContactDto>(dbContext), IContactReadRepository
{
    private static readonly Expression<Func<Contact, ContactDto>> ContactDtoProjection = x => new(
        x.Id.Value,
        x.OwnerUserId,
        x.ContactUserId,
        x.DisplayedName,
        x.FirstName,
        x.LastName,
        x.PhoneNumber == null ? null : x.PhoneNumber.Value,
        x.EmailAddress == null ? null : x.EmailAddress.Value,
        x.IsBlocked);

    protected override Expression<Func<Contact, ContactDto>> MapToDto => ContactDtoProjection;

    public async Task<IReadOnlyList<ContactDto>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var query = Query
            .Where(x => x.OwnerUserId == userId);

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(MapToDto)
            .ToListAsync(cancellationToken);
    }
}
