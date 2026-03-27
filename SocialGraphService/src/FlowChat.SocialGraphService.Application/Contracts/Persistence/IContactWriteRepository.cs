using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Domain.Entities;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IContactWriteRepository : IWriteRepository<Contact>
{
}

