using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Domain.Entities.Contact;

namespace FlowChat.SocialGraphService.Application.Contracts.Persistence;

public interface IContactWriteRepository : IWriteRepository<Contact>
{
}

