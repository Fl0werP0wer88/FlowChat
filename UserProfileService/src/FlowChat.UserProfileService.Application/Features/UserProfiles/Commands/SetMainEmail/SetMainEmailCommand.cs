using FlowChat.Application.Abstractions;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.SetMainEmail;

public sealed record SetMainEmailCommand(Guid UserId, Guid EmailId) : ICommand<Guid>;
