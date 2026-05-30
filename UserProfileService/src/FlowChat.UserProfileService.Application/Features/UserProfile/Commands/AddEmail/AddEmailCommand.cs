using FlowChat.Shared.Application;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddEmail;

public sealed record AddEmailCommand(Guid UserId, Guid EmailId, string? Address)
    : ICommand<Guid>;
