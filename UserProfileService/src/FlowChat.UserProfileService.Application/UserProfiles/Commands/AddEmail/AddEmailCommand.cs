using FlowChat.Application.Abstractions;

namespace FlowChat.UserProfileService.Application.UserProfiles.Commands.AddEmail;

public sealed record AddEmailCommand(Guid UserId, string? Address) : ICommand<Guid>;
