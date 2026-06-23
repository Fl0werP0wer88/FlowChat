using FlowChat.PresenceService.Domain.Entities.UserPresencePreferences;
using FlowChat.Shared.Application;

namespace FlowChat.PresenceService.Application.Contracts.Persistence;

public interface IUserPresencePreferencesWriteRepository : IWriteRepository<UserPresencePreferences>
{
}
