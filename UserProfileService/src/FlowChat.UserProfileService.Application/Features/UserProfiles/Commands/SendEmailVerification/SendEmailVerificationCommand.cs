using FlowChat.Shared.Application;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.SendEmailVerification;

public sealed record SendEmailVerificationCommand(Guid UserId, Guid EmailId) : ICommand<Guid>;
