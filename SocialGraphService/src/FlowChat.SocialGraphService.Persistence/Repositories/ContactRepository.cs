using AutoMapper;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Persistence.Entities;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class ContactRepository : IContactRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;

    public ContactRepository(AppDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Contact> AddAsync(Contact entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.Contacts.AddAsync(_mapper.Map<ContactEntity>(entity), cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Contact entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Contacts.Update(_mapper.Map<ContactEntity>(entity));
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Contact entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Contacts.Remove(_mapper.Map<ContactEntity>(entity));
        await Task.CompletedTask;
    }
}
