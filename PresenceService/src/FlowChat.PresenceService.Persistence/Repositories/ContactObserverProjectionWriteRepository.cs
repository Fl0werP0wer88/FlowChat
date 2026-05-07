using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.PresenceService.Persistence.Repositories;

public sealed class ContactObserverProjectionWriteRepository(AppDbContext dbContext)
    : IContactObserverProjectionWriteRepository
{
    private const string ProjectionSource = "social-graph-contact-events";
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task InsertAsync(
        ContactObserverProjectionDto projection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projection);

        await _dbContext.ContactObserverProjections.AddAsync(
            new ContactObserverProjectionEntity
            {
                ObservedUserId = projection.ObservedUserId,
                ObserverUserId = projection.ObserverUserId,
                CreatedBy = ProjectionSource,
                CreatedAtUtc = projection.CreatedAtUtc,
                LastModifiedBy = ProjectionSource,
                LastModifiedAtUtc = projection.LastModifiedAtUtc
            },
            cancellationToken);
    }

    public Task<bool> ExistsAsync(
        Guid observedUserId,
        Guid observerUserId,
        CancellationToken cancellationToken = default) =>
        _dbContext.ContactObserverProjections.AnyAsync(
            x => x.ObservedUserId == observedUserId && x.ObserverUserId == observerUserId,
            cancellationToken);

    public async Task<bool> DeleteAsync(
        Guid observedUserId,
        Guid observerUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ContactObserverProjections.FirstOrDefaultAsync(
            x => x.ObservedUserId == observedUserId && x.ObserverUserId == observerUserId,
            cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _dbContext.ContactObserverProjections.Remove(entity);
        return true;
    }
}
