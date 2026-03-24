using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class ContactWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<Contact>(dbContext), IContactRepository;
