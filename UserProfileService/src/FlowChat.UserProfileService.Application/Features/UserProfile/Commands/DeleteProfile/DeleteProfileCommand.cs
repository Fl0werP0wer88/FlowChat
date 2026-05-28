using FlowChat.Shared.Application;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.DeleteProfile;

public sealed record DeleteProfileCommand(Guid UserId) : ICommand<Guid>;
