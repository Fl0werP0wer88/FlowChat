using FlowChat.Shared.Application;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetMainEmail;

public sealed record SetMainEmailCommand(Guid UserId, Guid EmailId) : ICommand<Guid>;

