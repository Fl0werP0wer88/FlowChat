using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class ContactRepository(AppDbContext dbContext) : IContactRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<Contact> AddAsync(Contact entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.Contacts.AddAsync(entity, cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Contact entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Contacts.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Contact entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Contacts.Remove(entity);
        await Task.CompletedTask;
    }
}
