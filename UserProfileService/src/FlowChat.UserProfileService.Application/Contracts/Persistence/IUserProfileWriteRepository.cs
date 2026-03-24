using FlowChat.Application.Abstractions;
using FlowChat.UserProfileService.Domain.Entities;

namespace FlowChat.UserProfileService.Application.Contracts.Persistence;

public interface IUserProfileWriteRepository : IWriteRepository<UserProfile>
{
}
