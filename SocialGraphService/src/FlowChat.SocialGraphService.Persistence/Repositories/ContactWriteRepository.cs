using FlowChat.Shared.Persistance;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities.Contact;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class ContactWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<Contact>(dbContext), IContactWriteRepository;

