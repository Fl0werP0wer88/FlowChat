using FlowChat.Shared.Application;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SendEmailVerification;

public sealed record SendEmailVerificationCommand(Guid UserId, Guid EmailId) : ICommand<Guid>;
