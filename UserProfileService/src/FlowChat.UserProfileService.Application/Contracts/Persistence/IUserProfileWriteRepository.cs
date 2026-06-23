using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;

namespace FlowChat.UserProfileService.Application.Contracts.Persistence;

public interface IUserProfileWriteRepository : IWriteRepository<UserProfile>
{
}

