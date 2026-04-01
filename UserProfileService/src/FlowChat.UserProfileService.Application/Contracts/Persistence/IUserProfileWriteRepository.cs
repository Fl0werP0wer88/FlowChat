using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Domain.Entities;

namespace FlowChat.UserProfileService.Application.Contracts.Persistence;

public interface IUserProfileWriteRepository : IWriteRepository<UserProfile>
{
}

