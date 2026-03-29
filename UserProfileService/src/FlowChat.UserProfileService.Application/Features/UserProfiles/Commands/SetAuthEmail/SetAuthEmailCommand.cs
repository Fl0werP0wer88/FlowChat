using FlowChat.Shared.Application;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.SetAuthEmail;

public sealed record SetAuthEmailCommand(Guid UserId, Guid EmailId) : ICommand<Guid>;
