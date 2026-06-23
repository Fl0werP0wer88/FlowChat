using FlowChat.Shared.Application;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetAuthEmail;

public sealed record SetAuthEmailCommand(Guid UserId, Guid EmailId) : ICommand<Guid>;
